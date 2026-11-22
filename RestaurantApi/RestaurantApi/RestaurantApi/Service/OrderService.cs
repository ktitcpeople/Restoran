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
    public class OrderService : IOrderService
    {
        private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "DineIn", "Delivery", "Takeaway"
        };

        private static readonly Dictionary<string, HashSet<string>> AllowedTransitions = new(StringComparer.OrdinalIgnoreCase)
        {
            ["New"] = new(StringComparer.OrdinalIgnoreCase) { "Preparing", "Cancelled" },
            ["Preparing"] = new(StringComparer.OrdinalIgnoreCase) { "Ready", "Cancelled" },
            ["Ready"] = new(StringComparer.OrdinalIgnoreCase) { "Paid", "Cancelled" }
        };

        private readonly ContextDb _context;
        private readonly IHubContext<KitchenHub> _hub;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ContextDb context, IHubContext<KitchenHub> hub, ILogger<OrderService> logger)
        {
            _context = context;
            _hub = hub;
            _logger = logger;
        }

        public async Task<IActionResult> CreateOrderAsync(CreateOrderRequest request)
        {
            if (request.Items == null || request.Items.Count == 0)
                return new BadRequestObjectResult(new { status = false, message = "Заказ пустой" });

            if (request.Items.Any(i => i.Quantity < 1))
                return new BadRequestObjectResult(new { status = false, message = "Количество порций должно быть больше 0" });

            var type = string.IsNullOrWhiteSpace(request.Type) ? "DineIn" : request.Type.Trim();
            var canonicalType = AllowedTypes.FirstOrDefault(t => t.Equals(type, StringComparison.OrdinalIgnoreCase));
            if (canonicalType == null)
                return new BadRequestObjectResult(new { status = false, message = "Некорректный тип заказа" });
            type = canonicalType;

            RestaurantTable? table = null;
            int branchId;
            if (type == "DineIn")
            {
                table = await _context.Tables.FirstOrDefaultAsync(t => t.TableID == request.TableID);
                if (table == null)
                    return new NotFoundObjectResult(new { status = false, message = "Стол не найден" });
                if (table.Status == "Occupied")
                    return new BadRequestObjectResult(new { status = false, message = "Стол уже занят" });
                branchId = table.BranchID;
            }
            else branchId = request.BranchID ?? _context.CurrentBranchID ?? 1;

            if (_context.IsBranchScopedRole && _context.CurrentBranchID != branchId)
                return new ForbidResult();
            var branch = await _context.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.BranchID == branchId && b.IsActive);
            if (branch == null)
                return new BadRequestObjectResult(new { status = false, message = "Выбранный филиал не найден или закрыт" });

            var requestedIds = request.Items.Select(i => i.MenuItemID).Distinct().ToList();
            var menu = await _context.MenuItems
                .Where(m => requestedIds.Contains(m.MenuItemID) && m.BranchID == branchId)
                .ToDictionaryAsync(m => m.MenuItemID);

            decimal total = 0;
            var items = new List<OrderItem>();
            foreach (var item in request.Items)
            {
                if (!menu.TryGetValue(item.MenuItemID, out var menuItem))
                    return new BadRequestObjectResult(new { status = false, message = $"Блюдо #{item.MenuItemID} не найдено" });
                if (!menuItem.IsAvailable)
                    return new BadRequestObjectResult(new { status = false, message = $"Блюдо «{menuItem.Name}» сейчас недоступно" });

                items.Add(new OrderItem { MenuItemID = item.MenuItemID, Quantity = item.Quantity });
                total += menuItem.Price * item.Quantity;
            }

            var subtotal = total;
            decimal discountPercent = 0;
            decimal discountAmount = 0;
            var discountReason = "";
            int? birthdayDiscountYear = null;
            if (request.CustomerUserID is int customerId)
            {
                var customer = await _context.Users.FindAsync(customerId);
                if (customer?.RoleId == 4)
                {
                    var previousOrders = await _context.Orders.CountAsync(o => o.CustomerUserID == customerId && o.Status != "Cancelled");
                    if (previousOrders == 0)
                    {
                        discountPercent = 10;
                        discountReason = "Скидка 10% на первый заказ";
                    }
                    else if ((previousOrders + 1) % 5 == 0)
                    {
                        discountPercent = 5;
                        discountReason = "Скидка 5% на каждый пятый заказ";
                    }

                    if (type == "DineIn" && customer.BirthDate.HasValue && TryGetBirthdayYear(customer.BirthDate.Value, DateTime.Today, out var eligibleBirthdayYear))
                    {
                        var alreadyUsed = await _context.Orders.AnyAsync(o => o.CustomerUserID == customerId && o.BirthdayDiscountYear == eligibleBirthdayYear);
                        if (!alreadyUsed)
                        {
                            birthdayDiscountYear = eligibleBirthdayYear;
                            if (discountPercent < 10)
                            {
                                discountPercent = 10;
                                discountReason = "Скидка 10% ко дню рождения";
                            }
                            else if (discountPercent == 10)
                                discountReason += "; акция ко дню рождения отмечена использованной";
                        }
                    }
                }
            }
            discountAmount = Math.Round(subtotal * discountPercent / 100m, 2, MidpointRounding.AwayFromZero);
            total = subtotal - discountAmount;

            var order = new Order
            {
                TableID = type == "DineIn" ? request.TableID : null,
                WaiterID = request.WaiterID,
                Type = type,
                PickupAddress = branch.Address,
                BranchID = branchId,
                Priority = request.Priority == 1 && type == "DineIn" ? 1 : 0,
                TotalPrice = total,
                CustomerUserID = request.CustomerUserID,
                DiscountAmount = discountAmount,
                DiscountPercent = discountPercent,
                DiscountReason = discountReason,
                BirthdayDiscountYear = birthdayDiscountYear,
                Items = items
            };

            if (table != null) table.Status = "Occupied";
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            if (table != null)
                await _hub.Clients.Group("TableObservers").SendAsync("TablesUpdated");

            await _hub.Clients.Group("Kitchen").SendAsync("NewOrder", new
            {
                order.OrderID,
                order.TableID,
                order.CreatedAt,
                order.Type,
                Items = items.Select(i => new { i.MenuItemID, i.Quantity })
            });
            await _hub.Clients.Group("Waiters").SendAsync("OrderUpdated");

            _logger.LogInformation("Создан заказ {OrderID} на стол {TableID}", order.OrderID, order.TableID);
            return new OkObjectResult(new { status = true, message = "Заказ создан", orderId = order.OrderID, total, subtotal, discountAmount, discountPercent, discountReason });
        }

        private static bool TryGetBirthdayYear(DateTime birthDate, DateTime today, out int birthdayYear)
        {
            for (var year = today.Year - 1; year <= today.Year + 1; year++)
            {
                var day = Math.Min(birthDate.Day, DateTime.DaysInMonth(year, birthDate.Month));
                var birthday = new DateTime(year, birthDate.Month, day);
                if (Math.Abs((today.Date - birthday).TotalDays) <= 3)
                {
                    birthdayYear = year;
                    return true;
                }
            }
            birthdayYear = 0;
            return false;
        }

        public async Task<IActionResult> UpdateStatusAsync(UpdateStatusRequest request)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.OrderID == request.OrderID);

            if (order == null)
                return new NotFoundObjectResult(new { status = false, message = "Заказ не найден" });

            var newStatus = request.Status?.Trim() ?? "";
            if (string.Equals(order.Status, newStatus, StringComparison.OrdinalIgnoreCase))
                return new OkObjectResult(new { status = true, message = "Статус уже установлен", orderId = order.OrderID, orderStatus = order.Status });

            if (!AllowedTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(newStatus))
                return new BadRequestObjectResult(new { status = false, message = $"Недопустимый переход статуса: {order.Status} → {newStatus}" });

            newStatus = allowed.First(s => s.Equals(newStatus, StringComparison.OrdinalIgnoreCase));

            if (order.Status == "New" && newStatus == "Preparing")
            {
                var menuItemIds = order.Items.Select(i => i.MenuItemID).Distinct().ToList();
                var techCards = await _context.TechCards
                    .Where(tc => menuItemIds.Contains(tc.MenuItemID))
                    .ToListAsync();
                var ingredientIds = techCards.Select(tc => tc.IngredientID).Distinct().ToList();
                var ingredients = await _context.Ingredients
                    .Where(i => ingredientIds.Contains(i.IngredientID))
                    .ToDictionaryAsync(i => i.IngredientID);

                var required = new Dictionary<int, decimal>();
                foreach (var item in order.Items)
                {
                    foreach (var tc in techCards.Where(tc => tc.MenuItemID == item.MenuItemID))
                    {
                        required[tc.IngredientID] = required.GetValueOrDefault(tc.IngredientID) + tc.Quantity * item.Quantity;
                    }
                }

                foreach (var pair in required)
                {
                    if (!ingredients.TryGetValue(pair.Key, out var ingredient)) continue;
                    if (ingredient.Stock < pair.Value)
                        return new BadRequestObjectResult(new { status = false, message = $"Недостаточно «{ingredient.Name}» на складе. Нужно {pair.Value} {ingredient.Unit}, доступно {ingredient.Stock} {ingredient.Unit}" });
                }

                foreach (var pair in required)
                {
                    if (ingredients.TryGetValue(pair.Key, out var ingredient))
                    {
                        ingredient.Stock -= pair.Value;
                        _context.StockTransactions.Add(new StockTransaction { IngredientID = ingredient.IngredientID, QuantityDelta = -pair.Value, Reason = $"Списание для заказа #{order.OrderID}" });
                    }
                }
            }

            if (newStatus == "Ready")
            {
                if (request.ActorRole == 3)
                {
                    order.ReadyByUserID = request.ActorUserID;
                    order.ReadyAt = DateTime.UtcNow;
                }
                await _hub.Clients.Group("Waiters").SendAsync("OrderReady", new { order.OrderID, order.TableID });
            }

            if (newStatus == "Preparing")
            {
                if (request.ActorRole == 3)
                {
                    order.PreparingByUserID = request.ActorUserID;
                    order.PreparingAt = DateTime.UtcNow;
                }
            }

            if (newStatus is "Paid" or "Cancelled")
            {
                if (newStatus == "Cancelled") order.BirthdayDiscountYear = null;
                if (newStatus == "Cancelled")
                {
                    var pendingPayments = await _context.PaymentTransactions
                        .Where(p => p.OrderID == order.OrderID && p.Status == "Pending")
                        .ToListAsync();
                    foreach (var payment in pendingPayments)
                        payment.Status = "Cancelled";
                }
                if (order.TableID.HasValue)
                {
                    var table = await _context.Tables.FindAsync(order.TableID.Value);
                    if (table != null)
                    {
                        var hasActiveReservation = await _context.Reservations.AnyAsync(r =>
                            r.TableID == table.TableID && r.Status == "Active" && r.DateTime >= DateTime.UtcNow.AddHours(-2));
                        table.Status = hasActiveReservation ? "Reserved" : "Free";
                    }
                }
            }

            order.Status = newStatus;
            await _context.SaveChangesAsync();
            if (order.TableID.HasValue && (newStatus is "Paid" or "Cancelled"))
                await _hub.Clients.Group("TableObservers").SendAsync("TablesUpdated");

            // Синхронизируем все рабочие экраны и клиента, отслеживающего заказ.
            await _hub.Clients.Group("Kitchen").SendAsync("OrderUpdated");
            await _hub.Clients.Group("Waiters").SendAsync("OrderUpdated");
            await _hub.Clients.Group($"Order_{order.OrderID}").SendAsync("OrderStatusChanged", new
            {
                order.OrderID,
                order.Status,
                message = newStatus switch
                {
                    "Preparing" => "Заказ готовится",
                    "Ready" => "Заказ готов",
                    "Paid" => "Заказ оплачен",
                    "Cancelled" => "Заказ отменён",
                    _ => $"Статус заказа: {newStatus}"
                }
            });

            _logger.LogInformation("Заказ {OrderID} → статус {Status}", order.OrderID, newStatus);
            return new OkObjectResult(new { status = true, message = "Статус обновлён", orderId = order.OrderID, orderStatus = order.Status });
        }

        public async Task<IActionResult> GetActiveOrdersAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .Where(o => o.Status != "Paid" && o.Status != "Cancelled")
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();

            return new OkObjectResult(new { status = true, orders });
        }

        public async Task<IActionResult> GetOrderByIdAsync(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.MenuItem)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
                return new NotFoundObjectResult(new { status = false, message = "Заказ не найден" });

            return new OkObjectResult(new { status = true, order });
        }
    }
}
