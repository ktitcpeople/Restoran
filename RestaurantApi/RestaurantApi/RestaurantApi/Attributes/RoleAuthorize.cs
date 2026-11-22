using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using RestaurantApi.Database;

namespace RestaurantApi.Attributes
{
    public class RoleAuthorizeAttributes : Attribute, IAsyncActionFilter
    {
        private readonly int[] _allowedRoles;

        public RoleAuthorizeAttributes(int[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var userPrincipal = context.HttpContext.User;
            if (userPrincipal.Identity?.IsAuthenticated != true)
            {
                context.Result = new UnauthorizedObjectResult(new { status = false, message = "Требуется авторизация" });
                return;
            }

            var userIdClaim = userPrincipal.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type.EndsWith("/nameidentifier"));
            var roleClaim = userPrincipal.Claims.FirstOrDefault(c => c.Type == "role" || c.Type.EndsWith("/role"));

            if (userIdClaim == null || roleClaim == null ||
                !int.TryParse(userIdClaim.Value, out var userId) ||
                !int.TryParse(roleClaim.Value, out var roleId))
            {
                context.Result = new UnauthorizedObjectResult(new { status = false, message = "Некорректный токен" });
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<ContextDb>();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserID == userId);

            if (user == null)
            {
                context.Result = new UnauthorizedObjectResult(new { status = false, message = "Пользователь не найден" });
                return;
            }

            if (user.RoleId != roleId || !_allowedRoles.Contains(user.RoleId))
            {
                context.Result = new ObjectResult(new { status = false, message = "Нет доступа" }) { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }

            await next();
        }
    }
}
