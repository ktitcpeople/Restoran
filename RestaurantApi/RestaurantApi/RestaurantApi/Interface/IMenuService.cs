using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Request;

namespace RestaurantApi.Interface
{
    public interface IMenuService
    {
        Task<IActionResult> GetAllAsync(int? branchId = null);
        Task<IActionResult> CreateAsync(CreateMenuItemRequest request);
        Task<IActionResult> UpdateAsync(int id, CreateMenuItemRequest request);
        Task<IActionResult> ToggleAvailabilityAsync(int id);
        Task<IActionResult> DeleteAsync(int id);
    }
}
