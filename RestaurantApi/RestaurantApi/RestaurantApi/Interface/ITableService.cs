using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Request;

namespace RestaurantApi.Interface
{
    public interface ITableService
    {
        Task<IActionResult> GetAllTablesAsync(int? branchId = null);
        Task<IActionResult> CreateTableAsync(CreateTableRequest request);
        Task<IActionResult> DeleteTableAsync(int id);
        Task<IActionResult> CreateReservationAsync(ReservationRequest request);
        Task<IActionResult> CancelReservationAsync(int id);
        Task<IActionResult> GetReservationsAsync();
    }
}
