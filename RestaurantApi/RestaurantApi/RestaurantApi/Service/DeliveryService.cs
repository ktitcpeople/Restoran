using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Hubs;
using RestaurantApi.Interface;
using RestaurantApi.Models;
using RestaurantApi.Request;

namespace RestaurantApi.Service
{
    public class DeliveryService : IDeliveryService
    {
        private readonly ContextDb _context;
        private readonly IHubContext<KitchenHub> _hub;
        private readonly ILogger<DeliveryService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DeliveryService(ContextDb context, IHubContext<KitchenHub> hub, ILogger<DeliveryService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        private int CurrentUserId => GetIntClaim("nameid", "/nameidentifier");
        private int CurrentRole => GetIntClaim("role", "/role");

        private int GetIntClaim(string exact, string suffix)
        {
            var claim = _httpContextAccessor.HttpContext?.User.Claims.FirstOrDefault(c => c.Type == exact || c.Type.EndsWith(suffix));
            return int.TryParse(claim?.Value, out var value) ? value : 0;
        }

        public async Task<IActionResult> CreateDeliveryAsync(CreateDeliveryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Address) || string.IsNullOrWhiteSpace(request.ClientPhone))
                return new BadRequestObjectResult(new { status = false, message = "Укажите адрес и телефон" });

            var order = await _context.Orders.FindAsync(request.OrderID);
            if (order == null)
                return new NotFoundObjectResult(new { status = false, message = "Заказ не найден" });
            if (order.Type != "Delivery")
                return new BadRequestObjectResult(new { status = false, message = "Доставку можно создать только для заказа типа Delivery" });
            if (await _context.Deliveries.AnyAsync(d => d.OrderID == request.OrderID))
                return new BadRequestObjectResult(new { status = false, message = "Доставка для этого заказа уже создана" });

            // Гость может создать доставку только для собственного заказа.
            if (CurrentRole is (4 or 6) && order.WaiterID != CurrentUserId)
                return new ObjectResult(new { status = false, message = "Нет доступа к этому заказу" }) { StatusCode = StatusCodes.Status403Forbidden };

            var delivery = new Delivery
            {
                OrderID = request.OrderID,
                ClientPhone = request.ClientPhone.Trim(),
                Address = request.Address.Trim()
            };

            await _context.Deliveries.AddAsync(delivery);
            await _context.SaveChangesAsync();

            // Курьеры получают заказ сразу после его оформления и могут выдвинуться к филиалу,
            // пока кухня готовит заказ. Неназначенную доставку сможет принять один курьер.
            await _hub.Clients.Group("Couriers").SendAsync("NewDelivery", new
            {
                delivery.DeliveryID,
                delivery.OrderID,
                pickupAddress = order.PickupAddress,
                delivery.Address,
                delivery.ClientPhone,
                message = $"Новый заказ #{delivery.OrderID}: забрать в филиале «{order.PickupAddress}» и доставить клиенту."
            });

            _logger.LogInformation("Создана доставка {DeliveryID} для заказа {OrderID}", delivery.DeliveryID, request.OrderID);
            return new OkObjectResult(new { status = true, message = "Доставка создана", deliveryId = delivery.DeliveryID });
        }

        public async Task<IActionResult> AssignCourierAsync(AssignCourierRequest request)
        {
            var delivery = await _context.Deliveries.FindAsync(request.DeliveryID);
            if (delivery == null)
                return new NotFoundObjectResult(new { status = false, message = "Доставка не найдена" });
            if (delivery.Status != "Pending")
                return new BadRequestObjectResult(new { status = false, message = "Эта доставка уже назначена" });

            var courier = await _context.Users.FirstOrDefaultAsync(u => u.UserID == request.CourierID && u.RoleId == 5);
            if (courier == null)
                return new BadRequestObjectResult(new { status = false, message = "Курьер не найден" });

            delivery.CourierID = request.CourierID;
            delivery.Status = "Assigned";
            delivery.EstimatedArrival = DateTime.UtcNow.AddMinutes(30);

            await _context.SaveChangesAsync();

            await _hub.Clients.Group($"Courier_{request.CourierID}").SendAsync("NewDelivery", new
            {
                delivery.DeliveryID,
                delivery.Address,
                delivery.ClientPhone,
                delivery.OrderID,
                estimatedArrival = delivery.EstimatedArrival
            });

            await _hub.Clients.Group($"Order_{delivery.OrderID}").SendAsync("DeliveryAssigned", new
            {
                delivery.DeliveryID,
                courierName = courier.UserName,
                estimatedArrival = delivery.EstimatedArrival,
                message = $"Курьер {courier.UserName} назначен. Ожидаемое время прибытия: {delivery.EstimatedArrival:HH:mm}"
            });

            _logger.LogInformation("Курьер {CourierID} назначен на доставку {DeliveryID}", request.CourierID, request.DeliveryID);
            return new OkObjectResult(new { status = true, message = "Курьер назначен", estimatedArrival = delivery.EstimatedArrival });
        }

        public async Task<IActionResult> UpdateStatusAsync(UpdateDeliveryStatusRequest request)
        {
            var delivery = await _context.Deliveries.Include(d => d.Courier).FirstOrDefaultAsync(d => d.DeliveryID == request.DeliveryID);
            if (delivery == null)
                return new NotFoundObjectResult(new { status = false, message = "Доставка не найдена" });

            var isClaimAttempt = CurrentRole == 5 && delivery.CourierID == null && delivery.Status == "Pending" &&
                string.Equals(request.Status, "Accepted", StringComparison.OrdinalIgnoreCase);
            if (isClaimAttempt)
            {
                var claimed = await _context.Deliveries
                    .Where(d => d.DeliveryID == request.DeliveryID && d.CourierID == null && d.Status == "Pending")
                    .ExecuteUpdateAsync(updates => updates
                        .SetProperty(d => d.CourierID, CurrentUserId)
                        .SetProperty(d => d.Status, "Assigned"));
                if (claimed == 0)
                    return new ConflictObjectResult(new { status = false, message = "Эту доставку уже принял другой курьер" });

                delivery.CourierID = CurrentUserId;
                delivery.Status = "Assigned";
            }

            if (CurrentRole == 5 && delivery.CourierID != CurrentUserId)
                return new ObjectResult(new { status = false, message = "Доставка назначена другому курьеру" }) { StatusCode = StatusCodes.Status403Forbidden };

            var allowedNext = delivery.Status switch
            {
                "Assigned" => "Accepted",
                "Accepted" => "InTransit",
                "InTransit" => "Delivered",
                _ => null
            };

            if (!string.Equals(request.Status, allowedNext, StringComparison.OrdinalIgnoreCase))
                return new BadRequestObjectResult(new { status = false, message = $"Недопустимый переход статуса доставки: {delivery.Status} → {request.Status}" });

            var newStatus = allowedNext!;

            if (newStatus == "InTransit")
            {
                if (!request.EstimatedMinutes.HasValue || request.EstimatedMinutes is < 1 or > 180)
                    return new BadRequestObjectResult(new { status = false, message = "Укажите время прибытия от 1 до 180 минут" });
                delivery.EstimatedArrival = DateTime.UtcNow.AddMinutes(request.EstimatedMinutes.Value);
            }

            delivery.Status = newStatus;
            await _context.SaveChangesAsync();

            var statusMessage = newStatus switch
            {
                "Accepted" => "Курьер принял заказ и готовится к выезду",
                "InTransit" => $"Курьер выехал. Ожидаемое время прибытия: {delivery.EstimatedArrival:HH:mm}",
                "Delivered" => "Заказ доставлен!",
                _ => $"Статус доставки обновлён: {newStatus}"
            };

            await _hub.Clients.Group($"Order_{delivery.OrderID}").SendAsync("DeliveryStatusChanged", new
            {
                delivery.DeliveryID,
                delivery.Status,
                estimatedArrival = delivery.EstimatedArrival,
                message = statusMessage
            });

            _logger.LogInformation("Доставка {DeliveryID} → статус {Status}", request.DeliveryID, newStatus);
            return new OkObjectResult(new { status = true, message = "Статус доставки обновлён", deliveryStatus = delivery.Status, estimatedArrival = delivery.EstimatedArrival });
        }

        public async Task<IActionResult> GetAllAsync()
        {
            var query = _context.Deliveries
                .Include(d => d.Order)
                .Include(d => d.Courier)
                .AsQueryable();

            if (CurrentRole == 5)
                query = query.Where(d => d.CourierID == CurrentUserId || (d.CourierID == null && d.Status == "Pending"));

            var deliveries = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return new OkObjectResult(new { status = true, deliveries });
        }
    }
}
