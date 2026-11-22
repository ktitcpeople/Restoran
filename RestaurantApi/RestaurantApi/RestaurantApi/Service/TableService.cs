using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Interface;
using RestaurantApi.Hubs;
using RestaurantApi.Models;
using RestaurantApi.Request;

namespace RestaurantApi.Service
{
    public class TableService : ITableService
    {
        private readonly ContextDb _context;
        private readonly ILogger<TableService> _logger;
        private readonly IHubContext<KitchenHub> _hub;

        public TableService(ContextDb context, ILogger<TableService> logger, IHubContext<KitchenHub> hub)
        {
            _context = context;
            _logger = logger;
            _hub = hub;
        }

        public async Task<IActionResult> GetAllTablesAsync(int? branchId = null)
        {
            var query = _context.Tables.AsQueryable();
            if (branchId.HasValue) query = query.Where(t => t.BranchID == branchId.Value);
            var tables = await query.OrderBy(t => t.Number).ToListAsync();
            return new OkObjectResult(new { status = true, tables });
        }

        public async Task<IActionResult> CreateTableAsync(CreateTableRequest request)
        {
            if (request.Number <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Номер стола должен быть больше 0" });
            if (request.Capacity <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Вместимость должна быть больше 0" });

            var branchId = _context.CurrentBranchID ?? 1;
            if (await _context.Tables.AnyAsync(t => t.BranchID == branchId && t.Number == request.Number))
                return new BadRequestObjectResult(new { status = false, message = "Стол с таким номером уже существует" });

            var table = new RestaurantTable
            {
                Number = request.Number,
                Capacity = request.Capacity,
                PositionX = Math.Clamp(request.PositionX, 1, 12),
                PositionY = Math.Clamp(request.PositionY, 1, 12),
                Status = "Free",
                BranchID = branchId
            };
            await _context.Tables.AddAsync(table);
            await _context.SaveChangesAsync();
            await _hub.Clients.Group("TableObservers").SendAsync("TablesUpdated");
            return new OkObjectResult(new { status = true, message = "Стол создан", tableId = table.TableID });
        }

        public async Task<IActionResult> DeleteTableAsync(int id)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.TableID == id);
            if (table == null) return new NotFoundObjectResult(new { status = false, message = "Стол не найден" });

            var hasOrders = await _context.Orders.AnyAsync(o => o.TableID == id && o.Status != "Paid" && o.Status != "Cancelled");
            if (hasOrders)
                return new BadRequestObjectResult(new { status = false, message = "У стола есть активные заказы" });

            var hasReservations = await _context.Reservations.AnyAsync(r => r.TableID == id && r.Status == "Active");
            if (hasReservations)
                return new BadRequestObjectResult(new { status = false, message = "У стола есть активные бронирования" });

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();
            await _hub.Clients.Group("TableObservers").SendAsync("TablesUpdated");
            return new OkObjectResult(new { status = true, message = "Стол удалён" });
        }

        public async Task<IActionResult> GetReservationsAsync()
        {
            var reservations = await _context.Reservations
                .Include(r => r.Table)
                .Where(r => r.Status == "Active")
                .OrderBy(r => r.DateTime)
                .ToListAsync();
            return new OkObjectResult(new { status = true, reservations });
        }

        public async Task<IActionResult> CreateReservationAsync(ReservationRequest request)
        {
            var table = await _context.Tables.FirstOrDefaultAsync(t => t.TableID == request.TableID);
            if (table == null)
                return new NotFoundObjectResult(new { status = false, message = "Стол не найден" });

            request.GuestName = request.GuestName?.Trim() ?? "";
            request.GuestPhone = request.GuestPhone?.Trim() ?? "";
            if (request.Persons <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Количество гостей должно быть больше нуля" });
            if (request.Persons > table.Capacity)
                return new BadRequestObjectResult(new { status = false, message = "Количество гостей превышает вместимость стола" });
            if (string.IsNullOrWhiteSpace(request.GuestName) || string.IsNullOrWhiteSpace(request.GuestPhone))
                return new BadRequestObjectResult(new { status = false, message = "Укажите имя и телефон гостя" });

            var dt = request.DateTime.Kind switch
            {
                DateTimeKind.Utc => request.DateTime,
                DateTimeKind.Local => request.DateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(request.DateTime, DateTimeKind.Utc)
            };

            if (dt < DateTime.UtcNow)
                return new BadRequestObjectResult(new { status = false, message = "Дата бронирования не может быть в прошлом" });

            // Проверка конфликта — ±2 часа
            var from = dt.AddHours(-2);
            var to = dt.AddHours(2);
            var hasConflict = await _context.Reservations.AnyAsync(r =>
                r.TableID == request.TableID &&
                r.DateTime >= from && r.DateTime <= to &&
                r.Status == "Active");

            if (hasConflict)
                return new BadRequestObjectResult(new { status = false, message = "Стол уже забронирован на это время" });

            var res = new Reservation
            {
                TableID = request.TableID,
                GuestName = request.GuestName,
                GuestPhone = request.GuestPhone,
                DateTime = dt,
                Persons = request.Persons
            };

            table.Status = "Reserved";
            await _context.Reservations.AddAsync(res);
            await _context.SaveChangesAsync();
            await _hub.Clients.Group("TableObservers").SendAsync("TablesUpdated");

            _logger.LogInformation("Бронь {ReservationID} создана для стола {TableID}", res.ReservationID, res.TableID);
            return new OkObjectResult(new { status = true, message = "Бронь создана", reservationId = res.ReservationID });
        }

        public async Task<IActionResult> CancelReservationAsync(int id)
        {
            var res = await _context.Reservations.Include(r => r.Table).FirstOrDefaultAsync(r => r.ReservationID == id);
            if (res == null)
                return new NotFoundObjectResult(new { status = false, message = "Бронь не найдена" });

            if (res.Status == "Cancelled")
                return new OkObjectResult(new { status = true, message = "Бронь уже отменена" });

            res.Status = "Cancelled";
            if (res.Table != null && res.Table.Status != "Occupied")
            {
                var hasOtherReservation = await _context.Reservations.AnyAsync(r =>
                    r.TableID == res.TableID && r.ReservationID != res.ReservationID && r.Status == "Active");
                res.Table.Status = hasOtherReservation ? "Reserved" : "Free";
            }
            await _context.SaveChangesAsync();
            await _hub.Clients.Group("TableObservers").SendAsync("TablesUpdated");

            _logger.LogInformation("Бронь {ReservationID} отменена", id);
            return new OkObjectResult(new { status = true, message = "Бронь отменена" });
        }
    }
}
