using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Interface;

namespace RestaurantApi.Service
{
    public class ReportService : IReportService
    {
        private readonly ContextDb _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(ContextDb context, ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Отчёт по выручке за период (ТЗ §5.6)
        public async Task<IActionResult> GetRevenueReportAsync(DateTime from, DateTime to)
        {
            var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);
            var orders = await _context.Orders
                .Where(o => o.Status == "Paid" && o.CreatedAt >= fromUtc && o.CreatedAt < toUtc)
                .ToListAsync();

            var totalRevenue = orders.Sum(o => o.TotalPrice);
            var totalCost = await CalculateFoodCostAsync(orders.Select(o => o.OrderID).ToList());
            var orderCount = orders.Count;
            var averageCheck = orderCount > 0 ? totalRevenue / orderCount : 0;

            _logger.LogInformation("Сформирован отчёт по выручке с {From} по {To}", from, to);

            return new OkObjectResult(new
            {
                status = true,
                from,
                to,
                totalRevenue,
                totalCost,
                profit = totalRevenue - totalCost,
                orderCount,
                averageCheck = Math.Round(averageCheck, 2)
            });
        }

        // Популярные блюда (ТЗ §5.6)
        public async Task<IActionResult> GetPopularDishesAsync()
        {
            var popular = await _context.OrderItems
                .Include(oi => oi.MenuItem)
                .Where(oi => oi.Order.Status == "Paid")
                .GroupBy(oi => new { oi.MenuItemID, oi.MenuItem.Name, oi.MenuItem.Price })
                .Select(g => new
                {
                    g.Key.MenuItemID,
                    g.Key.Name,
                    g.Key.Price,
                    TotalOrdered = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.MenuItem.Price)
                })
                .OrderByDescending(x => x.TotalOrdered)
                .Take(10)
                .ToListAsync();

            return new OkObjectResult(new { status = true, popularDishes = popular });
        }

        // Остатки на складе (ТЗ §5.4)
        public async Task<IActionResult> GetStockReportAsync()
        {
            var ingredients = await _context.Ingredients.OrderBy(i => i.Stock).ToListAsync();
            var lowStock = ingredients.Where(i => i.Stock < 5).ToList();

            return new OkObjectResult(new
            {
                status = true,
                ingredients,
                lowStockCount = lowStock.Count,
                lowStockItems = lowStock
            });
        }

        // Себестоимость блюд (ТЗ §5.6)
        public async Task<IActionResult> GetCostReportAsync()
        {
            var techCards = await _context.TechCards
                .Include(tc => tc.MenuItem)
                .Include(tc => tc.Ingredient)
                .ToListAsync();

            var costByDish = techCards
                .GroupBy(tc => new { tc.MenuItemID, tc.MenuItem.Name, tc.MenuItem.Price })
                .Select(g => new
                {
                    g.Key.MenuItemID,
                    dishName = g.Key.Name,
                    sellingPrice = g.Key.Price,
                    cost = g.Sum(tc => tc.Quantity * tc.Ingredient.PricePerUnit),
                    profit = g.Key.Price - g.Sum(tc => tc.Quantity * tc.Ingredient.PricePerUnit),
                    margin = g.Key.Price > 0
                        ? Math.Round((g.Key.Price - g.Sum(tc => tc.Quantity * tc.Ingredient.PricePerUnit)) / g.Key.Price * 100, 1)
                        : 0
                })
                .OrderByDescending(x => x.margin)
                .ToList();

            return new OkObjectResult(new { status = true, dishes = costByDish });
        }

        // Загрузка кухни (ТЗ §5.6)
        public async Task<IActionResult> GetKitchenLoadReportAsync()
        {
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var orders = await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.CreatedAt >= today)
                .ToListAsync();

            // Загрузка по часам
            var byHour = orders
                .GroupBy(o => o.CreatedAt.Hour)
                .Select(g => new
                {
                    hour = g.Key,
                    orderCount = g.Count(),
                    itemCount = g.Sum(o => o.Items.Sum(i => i.Quantity))
                })
                .OrderBy(x => x.hour)
                .ToList();

            // Среднее время в статусе Preparing (загрузка кухни)
            var preparingOrders = orders.Where(o => o.Status == "Ready" || o.Status == "Paid").ToList();

            var statusStats = new
            {
                total = orders.Count,
                newCount = orders.Count(o => o.Status == "New"),
                preparingCount = orders.Count(o => o.Status == "Preparing"),
                readyCount = orders.Count(o => o.Status == "Ready"),
                paidCount = orders.Count(o => o.Status == "Paid"),
                cancelledCount = orders.Count(o => o.Status == "Cancelled")
            };

            return new OkObjectResult(new
            {
                status = true,
                date = today,
                byHour,
                statusStats
            });
        }

        // Загрузка зала (ТЗ §5.6)
        public async Task<IActionResult> GetHallLoadReportAsync()
        {
            var tables = await _context.Tables.ToListAsync();
            var totalTables = tables.Count;
            var occupiedTables = tables.Count(t => t.Status == "Occupied");
            var reservedTables = tables.Count(t => t.Status == "Reserved");
            var freeTables = tables.Count(t => t.Status == "Free");

            var occupancyRate = totalTables > 0
                ? Math.Round((double)(occupiedTables + reservedTables) / totalTables * 100, 1)
                : 0;

            // Бронирования на сегодня
            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var reservationsToday = await _context.Reservations
                .Include(r => r.Table)
                .Where(r => r.DateTime >= today && r.DateTime < today.AddDays(1) && r.Status == "Active")
                .OrderBy(r => r.DateTime)
                .ToListAsync();

            // Активные заказы в зале
            var dineInOrders = await _context.Orders
                .Where(o => o.Type == "DineIn" && o.Status != "Paid" && o.Status != "Cancelled")
                .CountAsync();

            return new OkObjectResult(new
            {
                status = true,
                totalTables,
                occupiedTables,
                reservedTables,
                freeTables,
                occupancyRate,
                dineInOrders,
                reservationsToday = reservationsToday.Select(r => new
                {
                    r.ReservationID,
                    tableNumber = r.Table?.Number,
                    r.GuestName,
                    r.Persons,
                    time = r.DateTime.ToString("HH:mm")
                })
            });
        }

        // Выручка за текущую смену (сегодня, с 06:00) (ТЗ §8.3)
        public async Task<IActionResult> GetShiftRevenueAsync()
        {
            var shiftStart = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddHours(6), DateTimeKind.Utc); // смена с 06:00
            if (DateTime.UtcNow.Hour < 6)
                shiftStart = DateTime.SpecifyKind(shiftStart.AddDays(-1), DateTimeKind.Utc); // ночная смена

            var orders = await _context.Orders
                .Where(o => o.Status == "Paid" && o.CreatedAt >= shiftStart)
                .ToListAsync();

            var totalRevenue = orders.Sum(o => o.TotalPrice);
            var totalCost = await CalculateFoodCostAsync(orders.Select(o => o.OrderID).ToList());
            var orderCount = orders.Count;
            var averageCheck = orderCount > 0 ? Math.Round(totalRevenue / orderCount, 2) : 0;

            return new OkObjectResult(new
            {
                status = true,
                shiftStart,
                totalRevenue,
                totalCost,
                profit = totalRevenue - totalCost,
                orderCount,
                averageCheck
            });
        }

        private async Task<decimal> CalculateFoodCostAsync(List<int> orderIds)
        {
            if (orderIds.Count == 0) return 0;
            var used = await _context.OrderItems.AsNoTracking().Where(i => orderIds.Contains(i.OrderID)).ToListAsync();
            var menuIds = used.Select(i => i.MenuItemID).Distinct().ToList();
            var recipes = await _context.TechCards.AsNoTracking().Include(t => t.Ingredient).Where(t => menuIds.Contains(t.MenuItemID)).ToListAsync();
            return used.Sum(line => line.Quantity * recipes.Where(t => t.MenuItemID == line.MenuItemID).Sum(t => t.Quantity * (t.Ingredient?.PricePerUnit ?? 0)));
        }

        public async Task<IActionResult> GetStaffPerformanceAsync(DateTime from, DateTime to)
        {
            var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            var toUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc);
            if (toUtc < fromUtc) return new BadRequestObjectResult(new { status = false, message = "Некорректный период." });
            var paidOrders = await _context.Orders.Where(o => o.Status == "Paid" && o.CreatedAt >= fromUtc && o.CreatedAt < toUtc).ToListAsync();
            var waiters = await _context.Users.Where(u => u.RoleId == 2).ToListAsync();
            var waiterStats = waiters.Select(u =>
            {
                var own = paidOrders.Where(o => o.WaiterID == u.UserID).ToList();
                return new { u.UserID, u.UserName, orderCount = own.Count, revenue = own.Sum(o => o.TotalPrice), averageCheck = own.Count == 0 ? 0 : Math.Round(own.Average(o => o.TotalPrice), 2) };
            }).OrderByDescending(x => x.revenue).ToList();
            var courierStats = await _context.Deliveries.Include(d => d.Courier).Where(d => d.CreatedAt >= fromUtc && d.CreatedAt < toUtc && d.CourierID != null)
                .GroupBy(d => new { d.CourierID, d.Courier.UserName })
                .Select(g => new { userID = g.Key.CourierID, userName = g.Key.UserName, assignedCount = g.Count(), deliveredCount = g.Count(d => d.Status == "Delivered") })
                .OrderByDescending(x => x.deliveredCount).ToListAsync();
            var cooks = await _context.Users.Where(u => u.RoleId == 3).ToListAsync();
            var cookStats = cooks.Select(u =>
            {
                var prepared = _context.Orders.AsNoTracking().Where(o => o.ReadyByUserID == u.UserID && o.ReadyAt >= fromUtc && o.ReadyAt <= toUtc && o.PreparingAt != null).ToList();
                var minutes = prepared.Where(o => o.ReadyAt.HasValue && o.PreparingAt.HasValue).Select(o => (o.ReadyAt!.Value - o.PreparingAt!.Value).TotalMinutes).ToList();
                return new { u.UserID, u.UserName, completedOrders = prepared.Count, averagePreparationMinutes = minutes.Count == 0 ? 0 : Math.Round(minutes.Average(), 1) };
            }).OrderByDescending(x => x.completedOrders).ToList();
            return new OkObjectResult(new { status = true, from, to, waiters = waiterStats, cooks = cookStats, couriers = courierStats });
        }
    }
}
