using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;

namespace RestaurantApi.Hubs
{
    [Authorize]
    public class KitchenHub : Hub
    {
        private readonly ContextDb _db;

        public KitchenHub(ContextDb db)
        {
            _db = db;
        }

        private int CurrentUserId
        {
            get
            {
                var claim = Context.User?.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type.EndsWith("/nameidentifier"));
                return int.TryParse(claim?.Value, out var id) ? id : 0;
            }
        }

        private int CurrentRole
        {
            get
            {
                var claim = Context.User?.Claims.FirstOrDefault(c => c.Type == "role" || c.Type.EndsWith("/role"));
                return int.TryParse(claim?.Value, out var role) ? role : 0;
            }
        }

        public async Task JoinKitchen()
        {
            if (CurrentRole is not (1 or 3)) throw new HubException("Нет доступа к группе кухни");
            await Groups.AddToGroupAsync(Context.ConnectionId, "Kitchen");
        }

        public async Task JoinWaiters()
        {
            if (CurrentRole is not (1 or 2)) throw new HubException("Нет доступа к группе официантов");
            await Groups.AddToGroupAsync(Context.ConnectionId, "Waiters");
        }

        public async Task JoinTableObservers()
        {
            if (CurrentRole is not (1 or 2 or 6)) throw new HubException("Нет доступа к обновлениям зала");
            await Groups.AddToGroupAsync(Context.ConnectionId, "TableObservers");
        }

        public async Task JoinCouriers()
        {
            if (CurrentRole is not (1 or 5)) throw new HubException("Нет доступа к группе курьеров");
            await Groups.AddToGroupAsync(Context.ConnectionId, "Couriers");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Courier_{CurrentUserId}");
        }

        public async Task JoinSupportOperators()
        {
            if (CurrentRole != 6) throw new HubException("Нет доступа к группе операторов поддержки");
            await Groups.AddToGroupAsync(Context.ConnectionId, "SupportOperators");
        }

        public async Task JoinSupportUser()
        {
            if (CurrentUserId <= 0) throw new HubException("Некорректный пользователь");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"SupportUser_{CurrentUserId}");
        }

        public async Task JoinOrderTracking(int orderId)
        {
            if (orderId <= 0) throw new HubException("Некорректный заказ");

            var allowed = CurrentRole switch
            {
                1 or 2 => await _db.Orders.AnyAsync(o => o.OrderID == orderId),
                4 or 6 => await _db.Orders.AnyAsync(o => o.OrderID == orderId && o.WaiterID == CurrentUserId),
                5 => await _db.Deliveries.AnyAsync(d => d.OrderID == orderId && d.CourierID == CurrentUserId),
                _ => false
            };

            if (!allowed) throw new HubException("Нет доступа к отслеживанию этого заказа");
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Order_{orderId}");
        }

        public async Task LeaveOrderTracking(int orderId)
        {
            if (orderId <= 0) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Order_{orderId}");
        }
    }
}
