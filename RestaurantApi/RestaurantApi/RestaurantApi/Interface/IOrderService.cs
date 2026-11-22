using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Request;

namespace RestaurantApi.Interface
{
    public interface IOrderService
    {
        Task<IActionResult> CreateOrderAsync(CreateOrderRequest request);
        Task<IActionResult> UpdateStatusAsync(UpdateStatusRequest request);
        Task<IActionResult> GetActiveOrdersAsync();
        Task<IActionResult> GetOrderByIdAsync(int id);
    }
}
