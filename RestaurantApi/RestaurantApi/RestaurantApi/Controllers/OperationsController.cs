using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Attributes;
using RestaurantApi.Database;
using RestaurantApi.Hubs;
using RestaurantApi.Models;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers;

public sealed class OperationsController(ContextDb db, IHubContext<KitchenHub> hub) : ControllerBase
{
    private int CurrentUserId => int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value, out var id) ? id : 0;

    [HttpGet, Route("/inventory/supplies"), RoleAuthorizeAttributes([1, 7])]
    public async Task<IActionResult> Supplies(CancellationToken ct)
    {
        var query = db.SupplyRequests.Include(x => x.Ingredient).Include(x => x.SupplierUser).AsQueryable();
        if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "role")?.Value, out var role) && role == 7)
            query = query.Where(x => x.SupplierUserID == CurrentUserId);
        var items = await query.OrderByDescending(x => x.CreatedAt).Select(x => new
        {
            x.SupplyRequestID, x.IngredientID, IngredientName = x.Ingredient!.Name,
            x.Quantity, x.Supplier, x.SupplierUserID, SupplierUserName = x.SupplierUser == null ? "Не назначен" : x.SupplierUser.UserName,
            x.Status, x.CreatedAt, x.ReceivedAt
        }).ToListAsync(ct);
        return Ok(new { status = true, items });
    }

    [HttpPost, Route("/inventory/supplies"), RoleAuthorizeAttributes([1])]
    public async Task<IActionResult> CreateSupply([FromBody] SupplyRequestCreate request, CancellationToken ct)
    {
        if (request.Quantity <= 0 || request.SupplierUserID <= 0) return BadRequest(new { status = false, message = "Выберите поставщика и укажите положительное количество." });
        var ingredient = await db.Ingredients.FirstOrDefaultAsync(x => x.IngredientID == request.IngredientID, ct);
        if (ingredient == null) return NotFound(new { status = false, message = "Ингредиент не найден." });
        var supplier = await db.Users.FirstOrDefaultAsync(x => x.UserID == request.SupplierUserID && x.RoleId == 7, ct);
        if (supplier == null) return BadRequest(new { status = false, message = "Выбранный пользователь не является поставщиком." });
        var row = new SupplyRequest { IngredientID = request.IngredientID, Quantity = request.Quantity, SupplierUserID = supplier.UserID, Supplier = supplier.UserName, Status = "Requested" };
        db.SupplyRequests.Add(row); await db.SaveChangesAsync(ct);
        return Ok(new { status = true, supplyRequestID = row.SupplyRequestID });
    }

    [HttpPost, Route("/inventory/supplies/{id:int}/accept"), RoleAuthorizeAttributes([7])]
    public async Task<IActionResult> AcceptSupply(int id, CancellationToken ct)
    {
        var request = await db.SupplyRequests.FirstOrDefaultAsync(x => x.SupplyRequestID == id && x.SupplierUserID == CurrentUserId, ct);
        if (request == null) return NotFound(new { status = false, message = "Заявка поставщика не найдена." });
        if (request.Status != "Requested") return BadRequest(new { status = false, message = "Можно принять только новую заявку." });
        request.Status = "Accepted";
        await db.SaveChangesAsync(ct);
        return Ok(new { status = true, message = "Заказ на поставку принят. После фактического поступления администратор отметит получение." });
    }

    [HttpPost, Route("/inventory/supplies/{id:int}/receive"), RoleAuthorizeAttributes([1])]
    public async Task<IActionResult> ReceiveSupply(int id, CancellationToken ct)
    {
        var request = await db.SupplyRequests.FirstOrDefaultAsync(x => x.SupplyRequestID == id, ct);
        if (request == null) return NotFound();
        if (request.Status == "Received") return BadRequest(new { status = false, message = "Поставка уже принята." });
        if (request.Status is not ("Requested" or "Accepted")) return BadRequest(new { status = false, message = "Заявка не может быть принята в текущем статусе." });
        var ingredient = await db.Ingredients.FirstOrDefaultAsync(x => x.IngredientID == request.IngredientID, ct);
        if (ingredient == null) return NotFound(new { status = false, message = "Ингредиент не найден." });
        ingredient.Stock += request.Quantity;
        request.Status = "Received"; request.ReceivedAt = DateTime.UtcNow;
        db.StockTransactions.Add(new StockTransaction { IngredientID = ingredient.IngredientID, UserID = CurrentUserId, QuantityDelta = request.Quantity, Reason = $"Поставка от {request.Supplier}" });
        await db.SaveChangesAsync(ct);
        await hub.Clients.Group("Waiters").SendAsync("StockUpdated", ct);
        return Ok(new { status = true, message = "Поставка принята и добавлена на склад." });
    }

    [HttpGet, Route("/inventory/stocktakes"), RoleAuthorizeAttributes([1])]
    public async Task<IActionResult> Stocktakes(CancellationToken ct)
        => Ok(new { status = true, items = await db.Stocktakes.Include(x => x.Items).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct) });

    [HttpPost, Route("/inventory/stocktakes"), RoleAuthorizeAttributes([1])]
    public async Task<IActionResult> CreateStocktake([FromBody] StocktakeRequest request, CancellationToken ct)
    {
        if (request.Items == null || request.Items.Count == 0 || request.Items.Any(x => x.CountedStock < 0)) return BadRequest(new { status = false, message = "Укажите фактические остатки (неотрицательные значения)." });
        var ids = request.Items.Select(x => x.IngredientID).Distinct().ToArray();
        var ingredients = await db.Ingredients.Where(x => ids.Contains(x.IngredientID)).ToDictionaryAsync(x => x.IngredientID, ct);
        if (ingredients.Count != ids.Length) return BadRequest(new { status = false, message = "В списке есть неизвестный ингредиент." });
        var take = new Stocktake { CreatedByUserID = CurrentUserId, Notes = request.Notes?.Trim() ?? "", BranchID = db.CurrentBranchID ?? 1 };
        foreach (var line in request.Items)
        {
            var ingredient = ingredients[line.IngredientID];
            var old = ingredient.Stock;
            ingredient.Stock = line.CountedStock;
            take.Items.Add(new StocktakeItem { IngredientID = line.IngredientID, ExpectedStock = old, CountedStock = line.CountedStock });
            if (old != line.CountedStock)
                db.StockTransactions.Add(new StockTransaction { IngredientID = line.IngredientID, UserID = CurrentUserId, QuantityDelta = line.CountedStock - old, Reason = "Корректировка по инвентаризации" });
        }
        db.Stocktakes.Add(take); await db.SaveChangesAsync(ct);
        await hub.Clients.Group("Waiters").SendAsync("StockUpdated", ct);
        return Ok(new { status = true, stocktakeID = take.StocktakeID });
    }

    [HttpGet, Route("/delivery/route-plan"), RoleAuthorizeAttributes([1, 2, 5])]
    public async Task<IActionResult> RoutePlan(CancellationToken ct)
    {
        var query = db.Deliveries.Include(x => x.Order).AsQueryable().Where(x => x.Status != "Delivered");
        if (int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "role")?.Value, out var role) && role == 5)
            query = query.Where(x => x.CourierID == CurrentUserId);
        return Ok(new { status = true, items = await query.OrderBy(x => x.RouteSequence).ThenBy(x => x.CreatedAt).ToListAsync(ct) });
    }

    [HttpPost, Route("/delivery/route-plan"), RoleAuthorizeAttributes([1, 2, 5])]
    public async Task<IActionResult> SetRoute([FromBody] RoutePlanRequest request, CancellationToken ct)
    {
        if (request.DeliveryIDs.Count == 0 || request.DeliveryIDs.Distinct().Count() != request.DeliveryIDs.Count) return BadRequest(new { status = false, message = "Маршрут должен содержать уникальные доставки." });
        var rows = await db.Deliveries.Where(x => request.DeliveryIDs.Contains(x.DeliveryID) && x.Status != "Delivered").ToListAsync(ct);
        if (rows.Count != request.DeliveryIDs.Count) return BadRequest(new { status = false, message = "Часть доставок недоступна." });
        var role = int.TryParse(User.Claims.FirstOrDefault(c => c.Type == "role")?.Value, out var r) ? r : 0;
        if (role == 5 && rows.Any(x => x.CourierID != CurrentUserId)) return Forbid();
        for (var i = 0; i < request.DeliveryIDs.Count; i++) rows.Single(x => x.DeliveryID == request.DeliveryIDs[i]).RouteSequence = i + 1;
        await db.SaveChangesAsync(ct);
        return Ok(new { status = true, message = "Порядок точек маршрута сохранён." });
    }
}
