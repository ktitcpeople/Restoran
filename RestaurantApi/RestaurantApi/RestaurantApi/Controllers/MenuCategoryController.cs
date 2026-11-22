using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Models;
using RestaurantApi.Attributes;

namespace RestaurantApi.Controllers
{
    [ApiController]
    public class MenuCategoryController : ControllerBase
    {
        private readonly ContextDb _db;
        public MenuCategoryController(ContextDb db) { _db = db; }

        public class CategoryRequest { public string Name { get; set; } = ""; }

        [HttpGet]
        [Route("/menu/categories")]
        public async Task<IActionResult> GetAll([FromQuery] int? branchId)
        {
            var targetBranch = _db.CurrentBranchID ?? branchId ?? 1;
            if (!await _db.Branches.IgnoreQueryFilters().AnyAsync(b => b.BranchID == targetBranch && b.IsActive))
                return BadRequest(new { status = false, message = "Филиал не найден" });
            // Самосиддинг: если категорий нет — заводим базовые
            if (!await _db.MenuCategories.IgnoreQueryFilters().AnyAsync(c => c.BranchID == targetBranch))
            {
                _db.MenuCategories.AddRange(
                    new MenuCategory { Name = "Супы", BranchID = targetBranch },
                    new MenuCategory { Name = "Салаты", BranchID = targetBranch },
                    new MenuCategory { Name = "Горячее", BranchID = targetBranch },
                    new MenuCategory { Name = "Гарниры", BranchID = targetBranch },
                    new MenuCategory { Name = "Напитки", BranchID = targetBranch },
                    new MenuCategory { Name = "Десерты", BranchID = targetBranch }
                );
                await _db.SaveChangesAsync();
            }

            var list = await _db.MenuCategories.Where(c => c.BranchID == targetBranch).OrderBy(c => c.Name).ToListAsync();
            return Ok(new { status = true, items = list });
        }

        [HttpPost]
        [Route("/menu/categories")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Create([FromBody] CategoryRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { status = false, message = "Название обязательно" });

            var name = req.Name.Trim();
            var branchId = _db.CurrentBranchID ?? 1;
            if (await _db.MenuCategories.AnyAsync(c => c.BranchID == branchId && c.Name.ToLower() == name.ToLower()))
                return BadRequest(new { status = false, message = "Такая категория уже есть" });

            var c = new MenuCategory { Name = name, BranchID = branchId };
            _db.MenuCategories.Add(c);
            await _db.SaveChangesAsync();
            return Ok(new { status = true, id = c.MenuCategoryID });
        }

        [HttpDelete]
        [Route("/menu/categories/{id}")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.MenuCategories.FirstOrDefaultAsync(x => x.MenuCategoryID == id);
            if (c == null) return NotFound(new { status = false });
            // Отвязываем все блюда от категории
            var items = await _db.MenuItems.Where(i => i.MenuCategoryID == id).ToListAsync();
            foreach (var i in items) i.MenuCategoryID = null;
            _db.MenuCategories.Remove(c);
            await _db.SaveChangesAsync();
            return Ok(new { status = true });
        }
    }
}
