using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Attributes;
using RestaurantApi.Database;
using RestaurantApi.Interface;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers
{
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ContextDb _db;

        public OrdersController(IOrderService orderService, ContextDb db)
        {
            _orderService = orderService;
            _db = db;
        }

        [HttpPost]
        [Route("/orders/create")]
        [RoleAuthorizeAttributes([1, 2, 4, 6])]
        public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type.EndsWith("/nameidentifier"));
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "role" || c.Type.EndsWith("/role"));
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
                return Unauthorized(new { status = false, message = "Некорректный токен" });

            // Идентификатор автора заказа берём из проверенного JWT, а не из тела запроса.
            request.WaiterID = userId;
            request.CustomerUserID = roleClaim?.Value == "4" ? userId : null;
            if (roleClaim?.Value == "4" || roleClaim?.Value == "6") request.Priority = 0;
            else request.Priority = request.Priority == 1 ? 1 : 0;
            return await _orderService.CreateOrderAsync(request);
        }

        [HttpPut]
        [Route("/orders/status")]
        [RoleAuthorizeAttributes([1, 2, 3])]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request)
        {
            request.ActorUserID = int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value, out var id) ? id : 0;
            request.ActorRole = int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "role")?.Value, out var role) ? role : 0;
            return await _orderService.UpdateStatusAsync(request);
        }

        [HttpGet]
        [Route("/orders/active")]
        [RoleAuthorizeAttributes([1, 2, 3])]
        public async Task<IActionResult> GetActive()
        {
            return await _orderService.GetActiveOrdersAsync();
        }

        [HttpGet]
        [Route("/orders/all")]
        [RoleAuthorizeAttributes([6])]
        public async Task<IActionResult> GetAllForSupport([FromQuery] int? orderId)
        {
            var query = _db.Orders.AsNoTracking()
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .AsQueryable();
            if (orderId.HasValue) query = query.Where(o => o.OrderID == orderId.Value);
            var orders = await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return Ok(new { status = true, orders });
        }

        [HttpGet]
        [Route("/orders/delivery-eligible")]
        [RoleAuthorizeAttributes([1, 2])]
        public async Task<IActionResult> GetDeliveryEligible()
        {
            var orders = await _db.Orders.AsNoTracking()
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .Where(o => o.Type == "Delivery" && o.Status != "Cancelled" && o.Status != "Paid" &&
                    !_db.Deliveries.Any(d => d.OrderID == o.OrderID))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
            return Ok(new { status = true, orders });
        }

        [HttpPost]
        [Route("/orders/cancel/{id:int}")]
        [RoleAuthorizeAttributes([6])]
        public async Task<IActionResult> CancelForSupport(int id)
        {
            return await _orderService.UpdateStatusAsync(new UpdateStatusRequest
            {
                OrderID = id,
                Status = "Cancelled",
                ActorRole = 6,
                ActorUserID = int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value, out var userId) ? userId : 0
            });
        }

        [HttpGet]
        [Route("/orders/get/{id}")]
        [RoleAuthorizeAttributes([1, 2, 3, 4, 6])]
        public async Task<IActionResult> GetById(int id)
        {
            var roleClaim = User.Claims.FirstOrDefault(c => c.Type == "role" || c.Type.EndsWith("/role"));
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type.EndsWith("/nameidentifier"));
            if (int.TryParse(roleClaim?.Value, out var role) && role is (4 or 6))
            {
                if (!int.TryParse(userIdClaim?.Value, out var userId) ||
                    !await _db.Orders.AsNoTracking().AnyAsync(o => o.OrderID == id && o.WaiterID == userId))
                    return StatusCode(StatusCodes.Status403Forbidden, new { status = false, message = "Нет доступа к этому заказу" });
            }

            return await _orderService.GetOrderByIdAsync(id);
        }
    }
}
