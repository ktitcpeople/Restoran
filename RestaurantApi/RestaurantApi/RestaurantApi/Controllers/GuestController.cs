using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Hubs;
using RestaurantApi.Attributes;

namespace RestaurantApi.Controllers
{
    [ApiController]
    public class GuestController : ControllerBase
    {
        private readonly ContextDb _db;
        private readonly IHubContext<KitchenHub> _hub;

        public GuestController(ContextDb db, IHubContext<KitchenHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        private int CurrentUserId
        {
            get
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type.EndsWith("/nameidentifier"));
                return int.TryParse(claim?.Value, out var id) ? id : 0;
            }
        }

        private int CurrentRole
        {
            get
            {
                var claim = User.Claims.FirstOrDefault(c => c.Type == "role" || c.Type.EndsWith("/role"));
                return int.TryParse(claim?.Value, out var role) ? role : 0;
            }
        }

        public class CallWaiterRequest
        {
            public int TableID { get; set; }
            public string? Reason { get; set; }
        }

        public class PayRequest
        {
            public int OrderID { get; set; }
        }

        [HttpGet]
        [Route("/guest/orders")]
        [RoleAuthorizeAttributes([4, 6])]
        public async Task<IActionResult> MyOrders()
        {
            var orders = await _db.Orders.AsNoTracking()
                .Include(o => o.Table)
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .Where(o => o.WaiterID == CurrentUserId && o.Status != "Cancelled")
                .OrderByDescending(o => o.CreatedAt)
                .Take(50)
                .ToListAsync();

            var orderIds = orders.Select(o => o.OrderID).ToList();
            var deliveries = await _db.Deliveries.AsNoTracking()
                .Where(d => orderIds.Contains(d.OrderID))
                .ToDictionaryAsync(d => d.OrderID);

            var items = orders.Select(o => new
            {
                o.OrderID,
                o.Status,
                o.Type,
                o.CreatedAt,
                o.TotalPrice,
                DeliveryStatus = deliveries.TryGetValue(o.OrderID, out var delivery) ? delivery.Status : null,
                EstimatedArrival = delivery?.EstimatedArrival,
                TableNumber = o.Table == null ? (int?)null : o.Table.Number,
                Items = o.Items.Select(i => new
                {
                    i.MenuItemID,
                    Name = i.MenuItem == null ? $"Блюдо #{i.MenuItemID}" : i.MenuItem.Name,
                    i.Quantity,
                    UnitPrice = i.MenuItem == null ? 0 : i.MenuItem.Price
                })
            });

            return Ok(new { status = true, items });
        }

        [HttpPost]
        [Route("/guest/call-waiter")]
        [RoleAuthorizeAttributes([4, 6])]
        public async Task<IActionResult> CallWaiter([FromBody] CallWaiterRequest req)
        {
            var table = await _db.Tables.FindAsync(req.TableID);
            if (table == null) return NotFound(new { status = false, message = "Стол не найден" });

            var hasOwnActiveOrder = await _db.Orders.AnyAsync(o =>
                o.TableID == req.TableID && o.WaiterID == CurrentUserId && o.Status != "Paid" && o.Status != "Cancelled");
            if (!hasOwnActiveOrder)
                return StatusCode(StatusCodes.Status403Forbidden, new { status = false, message = "Нет активного заказа за этим столом" });

            await _hub.Clients.Group("Waiters").SendAsync("WaiterCalled", new
            {
                tableID = req.TableID,
                tableNumber = table.Number,
                reason = req.Reason ?? "Гость зовёт официанта",
                at = DateTime.UtcNow
            });
            return Ok(new { status = true, message = "Официант позван" });
        }

        [HttpPost]
        [Route("/guest/request-bill")]
        [RoleAuthorizeAttributes([4, 6])]
        public async Task<IActionResult> RequestBill([FromBody] PayRequest req)
        {
            var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderID == req.OrderID);
            if (order == null) return NotFound(new { status = false, message = "Заказ не найден" });
            if (CurrentRole is (4 or 6) && order.WaiterID != CurrentUserId)
                return StatusCode(StatusCodes.Status403Forbidden, new { status = false, message = "Нет доступа к этому заказу" });

            await _hub.Clients.Group("Waiters").SendAsync("BillRequested", new
            {
                orderID = order.OrderID,
                tableID = order.TableID,
                total = order.TotalPrice,
                at = DateTime.UtcNow
            });
            return Ok(new { status = true, message = "Запрошена оплата", total = order.TotalPrice });
        }

        [HttpGet]
        [Route("/guest/order-by-table/{tableId}")]
        [RoleAuthorizeAttributes([1, 2, 4, 6])]
        public async Task<IActionResult> ActiveOrderForTable(int tableId)
        {
            var query = _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .Where(o => o.TableID == tableId && o.Status != "Paid" && o.Status != "Cancelled");

            if (CurrentRole is (4 or 6))
                query = query.Where(o => o.WaiterID == CurrentUserId);

            var order = await query.OrderByDescending(o => o.CreatedAt).FirstOrDefaultAsync();
            if (order == null) return Ok(new { status = true, order = (object?)null });
            return Ok(new
            {
                status = true,
                order = new
                {
                    orderID = order.OrderID,
                    tableID = order.TableID,
                    status = order.Status,
                    totalPrice = order.TotalPrice,
                    items = order.Items.Select(i => new
                    {
                        i.MenuItemID,
                        name = i.MenuItem != null ? i.MenuItem.Name : $"#{i.MenuItemID}",
                        i.Quantity
                    })
                }
            });
        }
    }
}
