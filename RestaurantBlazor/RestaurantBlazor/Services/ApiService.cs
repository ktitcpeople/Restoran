using RestaurantBlazor.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RestaurantBlazor.Services
{
    public class ApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<ApiService> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public ApiService(HttpClient http, ILogger<ApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        public void SetToken(string? token)
        {
            if (string.IsNullOrEmpty(token))
                _http.DefaultRequestHeaders.Authorization = null;
            else
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
        }

        // ===================== АВТОРИЗАЦИЯ =====================
        public async Task<LoginResponse?> LoginAsync(AuthRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/auth/login", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<LoginResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "Login error"); return null; }
        }

        public async Task<ApiResponse?> RegisterAsync(RegisterRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/auth/register", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "Register error"); return null; }
        }

        // ===================== МЕНЮ =====================
        public async Task<List<MenuItem>> GetMenuAsync(int? branchId = null)
        {
            try
            {
                var path = branchId.HasValue ? $"/menu/getall?branchId={branchId.Value}" : "/menu/getall";
                var resp = await _http.GetAsync(path);
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MenuResponse>(content, _jsonOptions);
                return result?.Items ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetMenu error"); return new(); }
        }

        public async Task<ApiResponse?> CreateMenuItemAsync(CreateMenuItemRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/menu/create", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> UpdateMenuItemAsync(int id, CreateMenuItemRequest request)
        {
            try
            {
                var resp = await _http.PutAsJsonAsync($"/menu/edit/{id}", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> DeleteMenuItemAsync(int id)
        {
            try
            {
                var resp = await _http.DeleteAsync($"/menu/delete/{id}");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> ToggleMenuItemAsync(int id)
        {
            try
            {
                var resp = await _http.PostAsync($"/menu/toggle/{id}", null);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<List<MenuCategory>> GetCategoriesAsync(int? branchId = null)
        {
            try
            {
                var path = branchId.HasValue ? $"/menu/categories?branchId={branchId.Value}" : "/menu/categories";
                var resp = await _http.GetAsync(path);
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<MenuCategoriesResponse>(content, _jsonOptions);
                return result?.Items ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetCategories error"); return new(); }
        }

        public async Task<ApiResponse?> CreateCategoryAsync(string name)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/menu/categories", new { Name = name });
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> DeleteCategoryAsync(int id)
        {
            try
            {
                var resp = await _http.DeleteAsync($"/menu/categories/{id}");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        // ===================== ЗАКАЗЫ =====================
        public async Task<List<Order>> GetActiveOrdersAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/orders/active");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<OrdersResponse>(content, _jsonOptions);
                return result?.Orders ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetActiveOrders error"); return new(); }
        }

        public async Task<List<Order>> GetDeliveryEligibleOrdersAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/orders/delivery-eligible");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<OrdersResponse>(content, _jsonOptions)?.Orders ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDeliveryEligibleOrders error"); return new(); }
        }

        public async Task<Order?> GetOrderByIdAsync(int id)
        {
            try
            {
                var resp = await _http.GetAsync($"/orders/get/{id}");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<SingleOrderResponse>(content, _jsonOptions);
                return result?.Order;
            }
            catch (Exception ex) { _logger.LogError(ex, "GetOrderById error"); return null; }
        }

        public async Task<ApiResponse?> CreateOrderAsync(CreateOrderRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/orders/create", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> UpdateOrderStatusAsync(UpdateStatusRequest request)
        {
            try
            {
                var resp = await _http.PutAsJsonAsync("/orders/status", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        // ===================== ГОСТЬ =====================
        public async Task<List<GuestOrderDto>> GetMyOrdersAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/guest/orders");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<GuestOrdersResponse>(content, _jsonOptions);
                return result?.Items ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetMyOrders error"); return new(); }
        }

        public async Task<ApiResponse?> CallWaiterAsync(int tableId, string? reason = null)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/guest/call-waiter", new { TableID = tableId, Reason = reason });
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> RequestBillAsync(int orderId)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/guest/request-bill", new { OrderID = orderId });
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<Order?> GetActiveOrderForTableAsync(int tableId)
        {
            try
            {
                var resp = await _http.GetAsync($"/guest/order-by-table/{tableId}");
                var content = await resp.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty("order", out var ord) || ord.ValueKind == JsonValueKind.Null)
                    return null;
                return JsonSerializer.Deserialize<Order>(ord.GetRawText(), _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetActiveOrderForTable error"); return null; }
        }

        // ===================== СТОЛЫ =====================
        public async Task<List<RestaurantTable>> GetTablesAsync(int? branchId = null)
        {
            try
            {
                var path = branchId.HasValue ? $"/tables/getall?branchId={branchId.Value}" : "/tables/getall";
                var resp = await _http.GetAsync(path);
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TablesResponse>(content, _jsonOptions);
                return result?.Tables ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetTables error"); return new(); }
        }

        public async Task<List<BranchDto>> GetBranchesAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/branches");
                var result = JsonSerializer.Deserialize<BranchesResponse>(await resp.Content.ReadAsStringAsync(), _jsonOptions);
                return result?.Items ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetBranches error"); return new(); }
        }

        public async Task<ApiResponse?> CreateTableAsync(CreateTableRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/tables/create", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> DeleteTableAsync(int id)
        {
            try
            {
                var resp = await _http.DeleteAsync($"/tables/delete/{id}");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<List<Reservation>> GetReservationsAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/tables/reservations");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ReservationsResponse>(content, _jsonOptions);
                return result?.Reservations ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetReservations error"); return new(); }
        }

        public async Task<ApiResponse?> CreateReservationAsync(ReservationRequest request)
        {
            try
            {
                var payload = new ReservationRequest
                {
                    TableID = request.TableID, Persons = request.Persons, GuestName = request.GuestName,
                    GuestPhone = request.GuestPhone, DateTime = request.DateTime.ToUniversalTime()
                };
                var resp = await _http.PostAsJsonAsync("/tables/reserve", payload);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> CancelReservationAsync(int id)
        {
            try
            {
                var resp = await _http.DeleteAsync($"/tables/reservations/cancel/{id}");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        // ===================== ДОСТАВКА =====================
        public async Task<List<Delivery>> GetDeliveriesAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/delivery/getall");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<DeliveriesResponse>(content, _jsonOptions);
                return result?.Deliveries ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetDeliveries error"); return new(); }
        }

        public async Task<ApiResponse?> CreateDeliveryAsync(CreateDeliveryRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/delivery/create", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> AssignCourierAsync(AssignCourierRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/delivery/assign", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> UpdateDeliveryStatusAsync(UpdateDeliveryStatusRequest request)
        {
            try
            {
                var resp = await _http.PutAsJsonAsync("/delivery/status", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<List<UserMini>> GetCouriersAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/auth/users?role=5");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<UsersMiniResponse>(content, _jsonOptions);
                return result?.Items ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetCouriers error"); return new(); }
        }

        public async Task<List<UserMini>> GetSuppliersAsync()
        {
            try
            {
                var r = await _http.GetAsync("/auth/users?role=7");
                return JsonSerializer.Deserialize<UsersMiniResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetSuppliers error"); return new(); }
        }

        public async Task<List<UserMini>> GetUsersAsync()
        {
            try { var r = await _http.GetAsync("/auth/users"); return JsonSerializer.Deserialize<UsersMiniResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new(); }
            catch { return new(); }
        }

        // ===================== СКЛАД =====================
        public async Task<List<Ingredient>> GetIngredientsAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/inventory/ingredients");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<IngredientsResponse>(content, _jsonOptions);
                return result?.Ingredients ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetIngredients error"); return new(); }
        }

        public async Task<ApiResponse?> CreateIngredientAsync(CreateIngredientRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/inventory/ingredients/create", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<ApiResponse?> AddStockAsync(AddStockRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/inventory/ingredients/stock", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        public async Task<List<TechCard>> GetTechCardsAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/inventory/techcards");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<TechCardsResponse>(content, _jsonOptions);
                return result?.TechCards ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetTechCards error"); return new(); }
        }

        public async Task<ApiResponse?> CreateTechCardAsync(CreateTechCardRequest request)
        {
            try
            {
                var resp = await _http.PostAsJsonAsync("/inventory/techcards", request);
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }

        // ===================== ОТЧЁТЫ =====================
        public async Task<RevenueReport?> GetRevenueReportAsync(DateTime from, DateTime to)
        {
            try
            {
                var resp = await _http.GetAsync($"/reports/revenue?from={from:yyyy-MM-ddTHH:mm:ss}&to={to:yyyy-MM-ddTHH:mm:ss}");
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GetRevenueReport returned {StatusCode}: {Response}", (int)resp.StatusCode, content);
                    return null;
                }
                return JsonSerializer.Deserialize<RevenueReport>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetRevenueReport error"); return null; }
        }

        public async Task<ShiftRevenue?> GetShiftRevenueAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/reports/shift-revenue");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ShiftRevenue>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetShiftRevenue error"); return null; }
        }

        public async Task<List<PopularDish>> GetPopularDishesAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/reports/popular-dishes");
                var content = await resp.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<PopularDishesResponse>(content, _jsonOptions);
                return result?.PopularDishes ?? new();
            }
            catch (Exception ex) { _logger.LogError(ex, "GetPopularDishes error"); return new(); }
        }

        public async Task<StockReport?> GetStockReportAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/reports/stock");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<StockReport>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetStockReport error"); return null; }
        }

        public async Task<CostReportResponse?> GetCostReportAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/reports/cost");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CostReportResponse>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetCostReport error"); return null; }
        }

        public async Task<KitchenLoadReport?> GetKitchenLoadAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/reports/kitchen-load");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<KitchenLoadReport>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetKitchenLoad error"); return null; }
        }

        public async Task<HallLoadReport?> GetHallLoadAsync()
        {
            try
            {
                var resp = await _http.GetAsync("/reports/hall-load");
                var content = await resp.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<HallLoadReport>(content, _jsonOptions);
            }
            catch (Exception ex) { _logger.LogError(ex, "GetHallLoad error"); return null; }
        }

        public async Task<SupportChatResponse?> SendSupportMessageAsync(SupportChatRequest request)
        {
            try { var r = await _http.PostAsJsonAsync("/support/chat", request); return JsonSerializer.Deserialize<SupportChatResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { _logger.LogError(ex, "Support message error"); return null; }
        }
        public async Task<List<SupportConversationDto>> GetSupportConversationsAsync()
        {
            try { var r = await _http.GetAsync("/support/conversations"); return JsonSerializer.Deserialize<SupportItemsResponse<SupportConversationDto>>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new(); }
            catch (Exception ex) { _logger.LogError(ex, "Support conversations error"); return new(); }
        }
        public async Task<List<SupportMessageDto>> GetSupportMessagesAsync(int id)
        {
            try { var r = await _http.GetAsync($"/support/conversations/{id}/messages"); return JsonSerializer.Deserialize<SupportMessagesResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Messages ?? new(); }
            catch (Exception ex) { _logger.LogError(ex, "Support messages error"); return new(); }
        }
        public async Task<List<SupportTicketDto>> GetSupportTicketsAsync()
        {
            try { var r = await _http.GetAsync("/support/tickets"); return JsonSerializer.Deserialize<SupportItemsResponse<SupportTicketDto>>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new(); }
            catch (Exception ex) { _logger.LogError(ex, "Support tickets error"); return new(); }
        }
        public async Task<ApiResponse?> ReplySupportAsync(int id, SupportReplyRequest request)
        {
            try { var r = await _http.PostAsJsonAsync($"/support/conversations/{id}/reply", request); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<List<SupplyRequestDto>> GetSupplyRequestsAsync()
        {
            try { var r = await _http.GetAsync("/inventory/supplies"); return JsonSerializer.Deserialize<SupportItemsResponse<SupplyRequestDto>>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new(); }
            catch { return new(); }
        }
        public async Task<ApiResponse?> CreateSupplyRequestAsync(SupplyRequestCreate request)
        {
            try { var r = await _http.PostAsJsonAsync("/inventory/supplies", request); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<ApiResponse?> ReceiveSupplyAsync(int id)
        {
            try { var r = await _http.PostAsync($"/inventory/supplies/{id}/receive", null); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<ApiResponse?> AcceptSupplyAsync(int id)
        {
            try { var r = await _http.PostAsync($"/inventory/supplies/{id}/accept", null); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<List<StocktakeDto>> GetStocktakesAsync()
        {
            try { var r = await _http.GetAsync("/inventory/stocktakes"); return JsonSerializer.Deserialize<SupportItemsResponse<StocktakeDto>>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new(); }
            catch { return new(); }
        }
        public async Task<ApiResponse?> CreateStocktakeAsync(StocktakeRequest request)
        {
            try { var r = await _http.PostAsJsonAsync("/inventory/stocktakes", request); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<StaffPerformanceResponse?> GetStaffPerformanceAsync(DateTime from, DateTime to)
        {
            try { var r = await _http.GetAsync($"/reports/staff-performance?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}"); return JsonSerializer.Deserialize<StaffPerformanceResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { _logger.LogError(ex, "Staff performance error"); return null; }
        }
        public async Task<ApiResponse?> CreateStaffAsync(RegisterRequest request)
        {
            try { var r = await _http.PostAsJsonAsync("/auth/users", request); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<List<Order>> GetOrdersForSupportAsync()
        {
            try { var r = await _http.GetAsync("/orders/all"); return JsonSerializer.Deserialize<OrdersResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Orders ?? new(); }
            catch (Exception ex) { _logger.LogError(ex, "GetOrdersForSupport error"); return new(); }
        }
        public async Task<ApiResponse?> CancelOrderForSupportAsync(int orderId)
        {
            try { var r = await _http.PostAsync($"/orders/cancel/{orderId}", null); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<ApiResponse?> ChangeUserRoleAsync(int userId, int role)
        {
            try { var r = await _http.PutAsJsonAsync($"/auth/users/{userId}/role", role); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<ApiResponse?> RequestPaymentAsync(StartPaymentRequest request)
        {
            try { var r = await _http.PostAsJsonAsync("/payments/request", request); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<OnlinePaymentStartResponse> StartOnlinePaymentAsync(int orderId)
        {
            try
            {
                var response = await _http.PostAsJsonAsync("/payments/online/create", new StartPaymentRequest { OrderID = orderId });
                return JsonSerializer.Deserialize<OnlinePaymentStartResponse>(await response.Content.ReadAsStringAsync(), _jsonOptions) ?? new() { Status = false, Message = "Пустой ответ API." };
            }
            catch (Exception ex) { return new() { Status = false, Message = ex.Message }; }
        }
        public async Task<OnlinePaymentStatusResponse> GetOnlinePaymentStatusAsync(int paymentId)
        {
            try
            {
                var response = await _http.GetAsync($"/payments/online/{paymentId}/status");
                return JsonSerializer.Deserialize<OnlinePaymentStatusResponse>(await response.Content.ReadAsStringAsync(), _jsonOptions) ?? new() { Status = false, Message = "Пустой ответ API." };
            }
            catch (Exception ex) { return new() { Status = false, Message = ex.Message }; }
        }
        public async Task<List<PaymentRequestDto>> GetPendingPaymentsAsync()
        {
            try { var r = await _http.GetAsync("/payments/pending"); return JsonSerializer.Deserialize<SupportItemsResponse<PaymentRequestDto>>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new(); }
            catch { return new(); }
        }
        public async Task<ApiResponse?> ConfirmPaymentAsync(int id)
        {
            try { var r = await _http.PostAsync($"/payments/{id}/confirm", null); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
        public async Task<List<Delivery>> GetRoutePlanAsync()
        {
            try { var r = await _http.GetAsync("/delivery/route-plan"); return JsonSerializer.Deserialize<SupportItemsResponse<Delivery>>(await r.Content.ReadAsStringAsync(), _jsonOptions)?.Items ?? new(); }
            catch { return new(); }
        }
        public async Task<ApiResponse?> SaveRoutePlanAsync(List<int> deliveryIds)
        {
            try { var r = await _http.PostAsJsonAsync("/delivery/route-plan", new RoutePlanRequest { DeliveryIDs = deliveryIds }); return JsonSerializer.Deserialize<ApiResponse>(await r.Content.ReadAsStringAsync(), _jsonOptions); }
            catch (Exception ex) { return new ApiResponse { Status = false, Message = ex.Message }; }
        }
    }
}
