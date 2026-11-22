using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Attributes;
using RestaurantApi.Interface;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers
{
    public class DeliveryController : ControllerBase
    {
        private readonly IDeliveryService _deliveryService;
        public DeliveryController(IDeliveryService deliveryService)
        {
            _deliveryService = deliveryService;
        }

        [HttpGet]
        [Route("/delivery/getall")]
        [RoleAuthorizeAttributes([1, 2, 5])]
        public async Task<IActionResult> GetAll()
        {
            return await _deliveryService.GetAllAsync();
        }

        [HttpPost]
        [Route("/delivery/create")]
        [RoleAuthorizeAttributes([1, 2, 4, 6])]
        public async Task<IActionResult> Create([FromBody] CreateDeliveryRequest request)
        {
            return await _deliveryService.CreateDeliveryAsync(request);
        }

        [HttpPost]
        [Route("/delivery/assign")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> Assign([FromBody] AssignCourierRequest request)
        {
            return await _deliveryService.AssignCourierAsync(request);
        }

        [HttpPut]
        [Route("/delivery/status")]
        [RoleAuthorizeAttributes([1, 5])]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateDeliveryStatusRequest request)
        {
            return await _deliveryService.UpdateStatusAsync(request);
        }
    }
}
