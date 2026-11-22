using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;
using RestaurantApi.Helpers;
using RestaurantApi.Interface;
using RestaurantApi.Models;
using RestaurantApi.Request;

namespace RestaurantApi.Service
{
    public class AuthService : IAuthService
    {
        private readonly ContextDb _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ContextDb context, IConfiguration config, ILogger<AuthService> logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        public async Task<IActionResult> RegisterAsync(RegisterRequest request)
        {
            request.Login = request.Login?.Trim() ?? "";
            request.UserName = request.UserName?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.UserName))
                return new BadRequestObjectResult(new { status = false, message = "Заполните имя, логин и пароль" });

            if (request.Password.Length < 6)
                return new BadRequestObjectResult(new { status = false, message = "Пароль должен содержать минимум 6 символов" });

            if (request.BirthDate.HasValue && request.BirthDate.Value.Date > DateTime.UtcNow.Date)
                return new BadRequestObjectResult(new { status = false, message = "Дата рождения не может быть в будущем." });

            if (request.Role is < 1 or > 7 || !await _context.Roles.AnyAsync(r => r.RoleId == request.Role))
                return new BadRequestObjectResult(new { status = false, message = "Некорректная роль" });

            if (!await _context.Branches.IgnoreQueryFilters().AnyAsync(b => b.BranchID == request.BranchID && b.IsActive))
                return new BadRequestObjectResult(new { status = false, message = "Выберите действующий филиал" });

            if (await _context.Users.IgnoreQueryFilters().AnyAsync(u => u.Login.ToLower() == request.Login.ToLower()))
                return new BadRequestObjectResult(new { status = false, message = "Логин уже занят" });

            var user = new User
            {
                UserName = request.UserName,
                RoleId = request.Role,
                Login = request.Login,
                Password = PasswordHelper.Hash(request.Password),
                BirthDate = request.BirthDate?.Date,
                BranchID = request.BranchID
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var jwtKey = _config["Jwt:Key"]!;
            var token = JWTGenerator.GenerateToken(user, jwtKey);

            user.Token = token;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Зарегистрирован пользователь {Login}, роль {Role}", request.Login, request.Role);

            return new OkObjectResult(new
            {
                status = true,
                message = "Пользователь создан",
                userId = user.UserID,
                userName = user.UserName,
                role = user.RoleId,
                branchId = user.BranchID,
                token
            });
        }

        public async Task<IActionResult> LoginAsync(AuthRequest request)
        {
            var login = request.Login?.Trim() ?? "";
            var normalizedLogin = login.ToLower();
            var user = await _context.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Login.ToLower() == normalizedLogin);

            if (user == null || !PasswordHelper.Verify(request.Password ?? "", user.Password))
            {
                _logger.LogWarning("Неудачная попытка входа для логина {Login}", login);
                return new UnauthorizedObjectResult(new { status = false, message = "Неверный логин или пароль" });
            }

            // Старые записи с открытым паролем автоматически переводятся на PBKDF2 при успешном входе.
            if (PasswordHelper.NeedsUpgrade(user.Password))
                user.Password = PasswordHelper.Hash(request.Password);

            var jwtKey = _config["Jwt:Key"]!;
            var token = JWTGenerator.GenerateToken(user, jwtKey);

            user.Token = token;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Пользователь {Login} вошёл в систему", login);

            return new OkObjectResult(new
            {
                status = true,
                token,
                userId = user.UserID,
                userName = user.UserName,
                role = user.RoleId,
                branchId = user.BranchID
            });
        }
    }
}
