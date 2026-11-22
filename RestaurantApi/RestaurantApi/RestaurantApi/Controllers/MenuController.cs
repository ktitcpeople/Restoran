using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Attributes;
using RestaurantApi.Interface;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers
{
    public class MenuController : ControllerBase
    {
        private readonly IMenuService _menuService;
        public MenuController(IMenuService menuService)
        {
            _menuService = menuService;
        }

        [HttpGet]
        [Route("/menu/getall")]
        public async Task<IActionResult> GetAll([FromQuery] int? branchId)
        {
            return await _menuService.GetAllAsync(branchId);
        }

        [HttpPost]
        [Route("/menu/create")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Create([FromBody] CreateMenuItemRequest request)
        {
            return await _menuService.CreateAsync(request);
        }

        [HttpPut]
        [Route("/menu/edit/{id}")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Update(int id, [FromBody] CreateMenuItemRequest request)
        {
            return await _menuService.UpdateAsync(id, request);
        }

        [HttpPost]
        [Route("/menu/toggle/{id}")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Toggle(int id)
        {
            return await _menuService.ToggleAvailabilityAsync(id);
        }

        [HttpDelete]
        [Route("/menu/delete/{id}")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Delete(int id)
        {
            return await _menuService.DeleteAsync(id);
        }
    }
}
