using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Attributes;
using RestaurantApi.Interface;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers
{
    public class InventoryController : ControllerBase
    {
        private readonly IInventoryService _inventoryService;
        public InventoryController(IInventoryService inventoryService)
        {
            _inventoryService = inventoryService;
        }

        [HttpGet]
        [Route("/inventory/ingredients")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetIngredients()
        {
            return await _inventoryService.GetIngredientsAsync();
        }

        [HttpPost]
        [Route("/inventory/ingredients/create")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> CreateIngredient([FromBody] CreateIngredientRequest request)
        {
            return await _inventoryService.CreateIngredientAsync(request);
        }

        [HttpPost]
        [Route("/inventory/ingredients/stock")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> AddStock([FromBody] AddStockRequest request)
        {
            return await _inventoryService.AddStockAsync(request);
        }

        [HttpGet]
        [Route("/inventory/techcards")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetTechCards()
        {
            return await _inventoryService.GetTechCardsAsync();
        }

        [HttpPost]
        [Route("/inventory/techcards")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> CreateTechCard([FromBody] CreateTechCardRequest request)
        {
            return await _inventoryService.CreateTechCardAsync(request);
        }
    }
}
