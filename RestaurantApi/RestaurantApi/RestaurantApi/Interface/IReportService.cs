using Microsoft.AspNetCore.Mvc;

namespace RestaurantApi.Interface
{
    public interface IReportService
    {
        Task<IActionResult> GetRevenueReportAsync(DateTime from, DateTime to);
        Task<IActionResult> GetPopularDishesAsync();
        Task<IActionResult> GetStockReportAsync();
        Task<IActionResult> GetCostReportAsync();          // себестоимость блюд
        Task<IActionResult> GetKitchenLoadReportAsync();   // загрузка кухни
        Task<IActionResult> GetHallLoadReportAsync();      // загрузка зала
        Task<IActionResult> GetShiftRevenueAsync();        // выручка за смену
        Task<IActionResult> GetStaffPerformanceAsync(DateTime from, DateTime to);
    }
}
