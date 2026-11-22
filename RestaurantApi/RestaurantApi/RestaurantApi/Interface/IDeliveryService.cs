using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Request;

namespace RestaurantApi.Interface
{
    public interface IDeliveryService
    {
        Task<IActionResult> CreateDeliveryAsync(CreateDeliveryRequest request);
        Task<IActionResult> AssignCourierAsync(AssignCourierRequest request);
        Task<IActionResult> UpdateStatusAsync(UpdateDeliveryStatusRequest request);
        Task<IActionResult> GetAllAsync();
    }
}
