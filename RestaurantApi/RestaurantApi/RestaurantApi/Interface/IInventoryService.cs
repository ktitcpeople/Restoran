using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Request;

namespace RestaurantApi.Interface
{
    public interface IInventoryService
    {
        Task<IActionResult> GetIngredientsAsync();
        Task<IActionResult> CreateIngredientAsync(CreateIngredientRequest request);
        Task<IActionResult> AddStockAsync(AddStockRequest request);
        Task<IActionResult> GetTechCardsAsync();
        Task<IActionResult> CreateTechCardAsync(CreateTechCardRequest request);
    }
}
