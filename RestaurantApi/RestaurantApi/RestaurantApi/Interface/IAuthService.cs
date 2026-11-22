using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Request;

namespace RestaurantApi.Interface
{
    public interface IAuthService
    {
        Task<IActionResult> RegisterAsync(RegisterRequest request);
        Task<IActionResult> LoginAsync(AuthRequest request);
    }
}
