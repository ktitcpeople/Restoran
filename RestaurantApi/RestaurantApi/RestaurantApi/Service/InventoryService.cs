using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Interface;
using RestaurantApi.Models;
using RestaurantApi.Request;

namespace RestaurantApi.Service
{
    public class InventoryService : IInventoryService
    {
        private readonly ContextDb _context;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(ContextDb context, ILogger<InventoryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> GetIngredientsAsync()
        {
            var ingredients = await _context.Ingredients.OrderBy(i => i.Name).ToListAsync();
            return new OkObjectResult(new { status = true, ingredients });
        }

        public async Task<IActionResult> CreateIngredientAsync(CreateIngredientRequest request)
        {
            var name = request.Name?.Trim() ?? "";
            var unit = request.Unit?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(unit))
                return new BadRequestObjectResult(new { status = false, message = "Укажите название и единицу измерения" });
            if (request.Stock < 0 || request.PricePerUnit < 0)
                return new BadRequestObjectResult(new { status = false, message = "Остаток и цена не могут быть отрицательными" });
            var branchId = _context.CurrentBranchID ?? 1;
            if (await _context.Ingredients.AnyAsync(i => i.BranchID == branchId && i.Name.ToLower() == name.ToLower()))
                return new BadRequestObjectResult(new { status = false, message = "Такой ингредиент уже существует" });

            var ingredient = new Ingredient { Name = name, Stock = request.Stock, Unit = unit, PricePerUnit = request.PricePerUnit, BranchID = branchId };
            await _context.Ingredients.AddAsync(ingredient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Добавлен ингредиент '{Name}'", ingredient.Name);
            return new OkObjectResult(new { status = true, message = "Ингредиент добавлен", ingredientId = ingredient.IngredientID });
        }

        public async Task<IActionResult> AddStockAsync(AddStockRequest request)
        {
            if (request.Amount <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Количество пополнения должно быть больше нуля" });

            var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngredientID == request.IngredientID);
            if (ingredient == null)
                return new NotFoundObjectResult(new { status = false, message = "Ингредиент не найден" });

            ingredient.Stock += request.Amount;
            _context.StockTransactions.Add(new StockTransaction { IngredientID = ingredient.IngredientID, QuantityDelta = request.Amount, Reason = "Пополнение склада" });
            await _context.SaveChangesAsync();

            _logger.LogInformation("Пополнен склад: '{Name}' +{Amount}", ingredient.Name, request.Amount);
            return new OkObjectResult(new { status = true, message = "Склад пополнен", newStock = ingredient.Stock });
        }

        public async Task<IActionResult> GetTechCardsAsync()
        {
            var techCards = await _context.TechCards
                .Include(tc => tc.MenuItem)
                .Include(tc => tc.Ingredient)
                .OrderBy(tc => tc.MenuItem!.Name)
                .ToListAsync();
            return new OkObjectResult(new { status = true, techCards });
        }

        public async Task<IActionResult> CreateTechCardAsync(CreateTechCardRequest request)
        {
            if (request.Quantity <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Количество ингредиента должно быть больше нуля" });

            var menuItem = await _context.MenuItems.FirstOrDefaultAsync(m => m.MenuItemID == request.MenuItemID);
            if (menuItem == null)
                return new NotFoundObjectResult(new { status = false, message = "Блюдо не найдено" });

            var ingredient = await _context.Ingredients.FirstOrDefaultAsync(i => i.IngredientID == request.IngredientID);
            if (ingredient == null)
                return new NotFoundObjectResult(new { status = false, message = "Ингредиент не найден" });

            var existing = await _context.TechCards.FirstOrDefaultAsync(tc =>
                tc.MenuItemID == request.MenuItemID && tc.IngredientID == request.IngredientID);

            if (existing != null)
            {
                existing.Quantity = request.Quantity;
                await _context.SaveChangesAsync();
                return new OkObjectResult(new { status = true, message = "Техкарта обновлена", techCardId = existing.TechCardID });
            }

            var techCard = new TechCard { MenuItemID = request.MenuItemID, IngredientID = request.IngredientID, Quantity = request.Quantity };
            await _context.TechCards.AddAsync(techCard);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Добавлена техкарта для блюда {MenuItemID}", request.MenuItemID);
            return new OkObjectResult(new { status = true, message = "Техкарта добавлена", techCardId = techCard.TechCardID });
        }
    }
}
