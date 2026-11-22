using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Attributes;
using RestaurantApi.Interface;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers
{
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ContextDb _db;

        public AuthController(IAuthService authService, ContextDb db)
        {
            _authService = authService;
            _db = db;
        }

        [HttpPost]
        [Route("/auth/register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
           // Публичная регистрация предназначена только для гостевых аккаунтов.
           request.Role = 4;
           if (!request.BirthDate.HasValue)
               return BadRequest(new { status = false, message = "Укажите дату рождения для участия в программе лояльности." });
           return await _authService.RegisterAsync(request);
        }

        [HttpPost]
        [Route("/auth/users")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> CreateStaff([FromBody] RegisterRequest request)
        {
            if (request.Role is < 1 or > 7 || request.Role == 4)
                return BadRequest(new { status = false, message = "Выберите допустимую роль сотрудника или поставщика." });
            request.BranchID = _db.CurrentBranchID ?? 1;
            return await _authService.RegisterAsync(request);
        }

        [HttpPut]
        [Route("/auth/users/{id:int}/role")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> ChangeRole(int id, [FromBody] int role)
        {
            if (role is < 1 or > 7) return BadRequest(new { status = false, message = "Некорректная роль." });
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null) return NotFound();
            user.RoleId = role;
            user.Token = null;
            await _db.SaveChangesAsync();
            return Ok(new { status = true, message = "Роль обновлена. Пользователю потребуется войти повторно." });
        }

        [HttpPost]
        [Route("/auth/login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest request)
        {
            return await _authService.LoginAsync(request);
        }

        [HttpGet]
        [Route("/auth/users")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Users([FromQuery] int? role)
        {
            var q = _db.Users.AsQueryable();
            if (role.HasValue) q = q.Where(u => u.RoleId == role.Value);
            var list = await q.OrderBy(u => u.UserName).Select(u => new
            {
                userID = u.UserID,
                userName = u.UserName,
                roleID = u.RoleId,
                branchID = u.BranchID,
                branchName = u.Branch!.Name
            }).ToListAsync();
            return Ok(new { status = true, items = list });
        }

        [HttpGet]
        [Route("/branches")]
        public async Task<IActionResult> Branches()
        {
            var branches = await _db.Branches.IgnoreQueryFilters().Where(b => b.IsActive).OrderBy(b => b.BranchID)
                .Select(b => new { b.BranchID, b.Name, b.Address }).ToListAsync();
            return Ok(new { status = true, items = branches });
        }
    }
}
