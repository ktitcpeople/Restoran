using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Interface;
using RestaurantApi.Models;
using RestaurantApi.Request;

namespace RestaurantApi.Service
{
    public class MenuService : IMenuService
    {
        private readonly ContextDb _context;
        private readonly ILogger<MenuService> _logger;

        public MenuService(ContextDb context, ILogger<MenuService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> GetAllAsync(int? branchId = null)
        {
            var query = _context.MenuItems.AsQueryable();
            if (branchId.HasValue) query = query.Where(m => m.BranchID == branchId.Value);
            var items = await query.OrderBy(m => m.Category).ToListAsync();
            return new OkObjectResult(new { status = true, items });
        }

        public async Task<IActionResult> CreateAsync(CreateMenuItemRequest request)
        {
            request.Name = request.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(request.Name))
                return new BadRequestObjectResult(new { status = false, message = "Укажите название блюда" });
            if (request.Price <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Цена должна быть больше нуля" });
            if (ImageValidationError(request.ImageData, request.ImageMimeType) is string imageError)
                return new BadRequestObjectResult(new { status = false, message = imageError });
            var branchId = _context.CurrentBranchID ?? 1;
            if (await _context.MenuItems.AnyAsync(m => m.BranchID == branchId && m.Name.ToLower() == request.Name.ToLower() && m.IsAvailable))
                return new BadRequestObjectResult(new { status = false, message = "Такое блюдо уже есть в меню" });

            string categoryName = request.Category ?? "";
            if (request.MenuCategoryID.HasValue)
            {
                var cat = await _context.MenuCategories.FirstOrDefaultAsync(c => c.MenuCategoryID == request.MenuCategoryID.Value);
                if (cat == null)
                    return new BadRequestObjectResult(new { status = false, message = "Категория не найдена" });
                categoryName = cat.Name;
            }
            if (string.IsNullOrWhiteSpace(categoryName))
                return new BadRequestObjectResult(new { status = false, message = "Выберите категорию" });

            var item = new MenuItem
            {
                Name = request.Name,
                Price = request.Price,
                Category = categoryName,
                MenuCategoryID = request.MenuCategoryID,
                KitchenStation = string.IsNullOrWhiteSpace(request.KitchenStation) ? "Общая кухня" : request.KitchenStation.Trim(),
                ImageUrl = request.ImageUrl?.Trim() ?? "",
                Description = request.Description?.Trim() ?? "",
                Protein = Math.Max(0, request.Protein), Fat = Math.Max(0, request.Fat), Carbohydrates = Math.Max(0, request.Carbohydrates)
                ,Calories = Math.Max(0, request.Calories), WeightGrams = Math.Max(0, request.WeightGrams),
                BranchID = branchId, ImageData = request.ImageData,
                ImageMimeType = request.ImageData is { Length: > 0 } ? request.ImageMimeType : ""
            };
            await _context.MenuItems.AddAsync(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Добавлено блюдо '{Name}'", item.Name);
            return new OkObjectResult(new { status = true, message = "Блюдо добавлено", menuItemId = item.MenuItemID });
        }

        public async Task<IActionResult> UpdateAsync(int id, CreateMenuItemRequest request)
        {
            request.Name = request.Name?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(request.Name))
                return new BadRequestObjectResult(new { status = false, message = "Укажите название блюда" });
            if (request.Price <= 0)
                return new BadRequestObjectResult(new { status = false, message = "Цена должна быть больше нуля" });
            if (ImageValidationError(request.ImageData, request.ImageMimeType) is string imageError)
                return new BadRequestObjectResult(new { status = false, message = imageError });

            var item = await _context.MenuItems.FirstOrDefaultAsync(m => m.MenuItemID == id);
            if (item == null)
                return new NotFoundObjectResult(new { status = false, message = "Блюдо не найдено" });

            string categoryName = request.Category ?? "";
            if (request.MenuCategoryID.HasValue)
            {
                var cat = await _context.MenuCategories.FirstOrDefaultAsync(c => c.MenuCategoryID == request.MenuCategoryID.Value);
                if (cat == null)
                    return new BadRequestObjectResult(new { status = false, message = "Категория не найдена" });
                categoryName = cat.Name;
            }
            if (string.IsNullOrWhiteSpace(categoryName))
                return new BadRequestObjectResult(new { status = false, message = "Выберите категорию" });

            item.Name = request.Name;
            item.Price = request.Price;
            item.Category = categoryName;
            item.MenuCategoryID = request.MenuCategoryID;
            item.KitchenStation = string.IsNullOrWhiteSpace(request.KitchenStation) ? "Общая кухня" : request.KitchenStation.Trim();
            item.ImageUrl = request.ImageUrl?.Trim() ?? "";
            item.Description = request.Description?.Trim() ?? "";
            item.Protein = Math.Max(0, request.Protein); item.Fat = Math.Max(0, request.Fat); item.Carbohydrates = Math.Max(0, request.Carbohydrates);
            item.Calories = Math.Max(0, request.Calories); item.WeightGrams = Math.Max(0, request.WeightGrams);
            if (request.ImageData is { Length: > 0 })
            {
                item.ImageData = request.ImageData;
                item.ImageMimeType = request.ImageMimeType;
            }
            await _context.SaveChangesAsync();

            return new OkObjectResult(new { status = true, message = "Блюдо обновлено" });
        }

        public async Task<IActionResult> DeleteAsync(int id)
        {
            var item = await _context.MenuItems.FirstOrDefaultAsync(m => m.MenuItemID == id);
            if (item == null)
                return new NotFoundObjectResult(new { status = false, message = "Блюдо не найдено" });

            item.IsAvailable = false; // Мягкое удаление
            await _context.SaveChangesAsync();

            _logger.LogInformation("Блюдо {MenuItemID} скрыто из меню", id);
            return new OkObjectResult(new { status = true, message = "Блюдо скрыто из меню" });
        }

        public async Task<IActionResult> ToggleAvailabilityAsync(int id)
        {
            var item = await _context.MenuItems.FirstOrDefaultAsync(m => m.MenuItemID == id);
            if (item == null)
                return new NotFoundObjectResult(new { status = false, message = "Блюдо не найдено" });

            item.IsAvailable = !item.IsAvailable;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Блюдо {MenuItemID} -> доступно={IsAvailable}", id, item.IsAvailable);
            return new OkObjectResult(new { status = true, isAvailable = item.IsAvailable, message = item.IsAvailable ? "Возвращено в меню" : "Скрыто из меню" });
        }

        private static string? ImageValidationError(byte[]? data, string mimeType)
        {
            if (data is not { Length: > 0 }) return null;
            if (data.Length > 5 * 1024 * 1024)
                return "Размер фотографии не должен превышать 5 МБ.";
            if (mimeType is not ("image/jpeg" or "image/png" or "image/webp"))
                return "Поддерживаются изображения JPEG, PNG и WebP.";
            return null;
        }
    }
}
