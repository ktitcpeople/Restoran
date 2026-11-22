using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Attributes;
using RestaurantApi.Interface;

namespace RestaurantApi.Controllers
{
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet]
        [Route("/reports/revenue")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetRevenue([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            // `to` задаётся клиентом как исключительная граница (начало следующего дня).
            if (from == default || to == default || from > to || from.Date > DateTime.Today || to > DateTime.Today.AddDays(1))
                return BadRequest(new { status = false, message = "Укажите корректный период: начало не позже окончания, даты не в будущем." });
            return await _reportService.GetRevenueReportAsync(from, to);
        }

        [HttpGet]
        [Route("/reports/popular-dishes")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetPopularDishes()
        {
            return await _reportService.GetPopularDishesAsync();
        }

        [HttpGet]
        [Route("/reports/stock")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetStock()
        {
            return await _reportService.GetStockReportAsync();
        }

        [HttpGet]
        [Route("/reports/cost")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetCost()
        {
            return await _reportService.GetCostReportAsync();
        }

        [HttpGet]
        [Route("/reports/kitchen-load")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetKitchenLoad()
        {
            return await _reportService.GetKitchenLoadReportAsync();
        }

        [HttpGet]
        [Route("/reports/hall-load")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetHallLoad()
        {
            return await _reportService.GetHallLoadReportAsync();
        }

        [HttpGet]
        [Route("/reports/shift-revenue")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetShiftRevenue()
        {
            return await _reportService.GetShiftRevenueAsync();
        }

        [HttpGet]
        [Route("/reports/staff-performance")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> GetStaffPerformance([FromQuery] DateTime from, [FromQuery] DateTime to)
        {
            // `to` задаётся клиентом как исключительная граница (начало следующего дня).
            if (from == default || to == default || from > to || from.Date > DateTime.Today || to > DateTime.Today.AddDays(1))
                return BadRequest(new { status = false, message = "Укажите корректный период: начало не позже окончания, даты не в будущем." });
            return await _reportService.GetStaffPerformanceAsync(from, to);
        }
    }
}
