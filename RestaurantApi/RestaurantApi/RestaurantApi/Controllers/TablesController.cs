using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantApi.Attributes;
using RestaurantApi.Interface;
using RestaurantApi.Request;

namespace RestaurantApi.Controllers
{
    public class TablesController : ControllerBase
    {
        private readonly ITableService _tableService;
        public TablesController(ITableService tableService)
        {
            _tableService = tableService;
        }

        [HttpGet]
        [Route("/tables/getall")]
        [RoleAuthorizeAttributes([1, 2, 4, 6])]
        public async Task<IActionResult> GetAll([FromQuery] int? branchId)
        {
            return await _tableService.GetAllTablesAsync(branchId);
        }

        [HttpGet]
        [Route("/tables/reservations")]
        [RoleAuthorizeAttributes([1, 2, 6])]
        public async Task<IActionResult> GetReservations()
        {
            return await _tableService.GetReservationsAsync();
        }

        [HttpPost]
        [Route("/tables/reserve")]
        [RoleAuthorizeAttributes([1, 2, 4, 6])]
        public async Task<IActionResult> Reserve([FromBody] ReservationRequest request)
        {
            return await _tableService.CreateReservationAsync(request);
        }

        [HttpPost]
        [Route("/tables/create")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> CreateTable([FromBody] CreateTableRequest request)
        {
            return await _tableService.CreateTableAsync(request);
        }

        [HttpDelete]
        [Route("/tables/delete/{id}")]
        [RoleAuthorizeAttributes([1])]
        public async Task<IActionResult> DeleteTable(int id)
        {
            return await _tableService.DeleteTableAsync(id);
        }

        [HttpDelete]
        [Route("/tables/reservations/cancel/{id}")]
        [RoleAuthorizeAttributes([1, 2, 6])]
        public async Task<IActionResult> CancelReservation(int id)
        {
            return await _tableService.CancelReservationAsync(id);
        }
    }
}
