namespace RestaurantBlazor.Models
{
    // ===================== БАЗОВЫЕ =====================
    public class ApiResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = "";
        public int OrderId { get; set; }
        public int DeliveryId { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public string DiscountReason { get; set; } = "";
    }

    // ===================== АВТОРИЗАЦИЯ =====================
    public class LoginResponse
    {
        public bool Status { get; set; }
        public string Token { get; set; } = "";
        public int UserId { get; set; }
        public string UserName { get; set; } = "";
        public int Role { get; set; }
        public int BranchId { get; set; } = 1;
    }

    public class UserDto
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = "";
        public int RoleId { get; set; }
        public string Login { get; set; } = "";
    }

    // ===================== МЕНЮ =====================
    public class MenuItem
    {
        public int MenuItemID { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public int? MenuCategoryID { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string KitchenStation { get; set; } = "Общая кухня";
        public string ImageUrl { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Calories { get; set; }
        public int WeightGrams { get; set; }
        public byte[]? ImageData { get; set; }
        public string ImageMimeType { get; set; } = "";
        public int BranchID { get; set; }
    }

    public class MenuResponse
    {
        public bool Status { get; set; }
        public List<MenuItem> Items { get; set; } = new();
    }

    public class MenuCategory
    {
        public int MenuCategoryID { get; set; }
        public string Name { get; set; } = "";
        public int BranchID { get; set; }
    }

    public class MenuCategoriesResponse
    {
        public bool Status { get; set; }
        public List<MenuCategory> Items { get; set; } = new();
    }

    public class UserMini
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = "";
        public int RoleID { get; set; }
        public int BranchID { get; set; }
        public string BranchName { get; set; } = "";
    }

    public class UsersMiniResponse
    {
        public bool Status { get; set; }
        public List<UserMini> Items { get; set; } = new();
    }

    // ===================== ЗАКАЗЫ =====================
    public class OrderItemDto
    {
        public int OrderItemID { get; set; }
        public int MenuItemID { get; set; }
        public int Quantity { get; set; }
        public MenuItem? MenuItem { get; set; }
    }

    public class Order
    {
        public int OrderID { get; set; }
        public int? TableID { get; set; }
        public int WaiterID { get; set; }
        public string Status { get; set; } = "New";
        public string Type { get; set; } = "DineIn";
        public string PickupAddress { get; set; } = "";
        public int Priority { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrdersResponse
    {
        public bool Status { get; set; }
        public List<Order> Orders { get; set; } = new();
    }

    public class SingleOrderResponse
    {
        public bool Status { get; set; }
        public Order? Order { get; set; }
    }

    public class GuestOrderItemDto
    {
        public int MenuItemID { get; set; }
        public string Name { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class GuestOrderDto
    {
        public int OrderID { get; set; }
        public string Status { get; set; } = "New";
        public string Type { get; set; } = "DineIn";
        public DateTime CreatedAt { get; set; }
        public decimal TotalPrice { get; set; }
        public int? TableNumber { get; set; }
        public string? DeliveryStatus { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public List<GuestOrderItemDto> Items { get; set; } = new();
    }

    public class GuestOrdersResponse
    {
        public bool Status { get; set; }
        public List<GuestOrderDto> Items { get; set; } = new();
    }

    // ===================== СТОЛЫ =====================
    public class RestaurantTable
    {
        public int TableID { get; set; }
        public int BranchID { get; set; }
        public int Number { get; set; }
        public int Capacity { get; set; }
        public int PositionX { get; set; } = 1;
        public int PositionY { get; set; } = 1;
        public string Status { get; set; } = "Free";
    }

    public class TablesResponse
    {
        public bool Status { get; set; }
        public List<RestaurantTable> Tables { get; set; } = new();
    }

    public class Reservation
    {
        public int ReservationID { get; set; }
        public int TableID { get; set; }
        public string GuestName { get; set; } = "";
        public string GuestPhone { get; set; } = "";
        public DateTime DateTime { get; set; }
        public int Persons { get; set; }
        public string Status { get; set; } = "Active";
        public RestaurantTable? Table { get; set; }
    }

    public class ReservationsResponse
    {
        public bool Status { get; set; }
        public List<Reservation> Reservations { get; set; } = new();
    }

    // ===================== ДОСТАВКА =====================
    public class Delivery
    {
        public int DeliveryID { get; set; }
        public int OrderID { get; set; }
        public int? CourierID { get; set; }
        public string ClientPhone { get; set; } = "";
        public string Address { get; set; } = "";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public DateTime? EstimatedArrival { get; set; }
        public int RouteSequence { get; set; }
        public Order? Order { get; set; }
        public UserDto? Courier { get; set; }
    }

    public sealed class BranchDto
    {
        public int BranchID { get; set; }
        public string Name { get; set; } = "";
        public string Address { get; set; } = "";
    }
    public sealed class BranchesResponse { public bool Status { get; set; } public List<BranchDto> Items { get; set; } = new(); }

    public sealed class OnlinePaymentStartResponse { public bool Status { get; set; } public int PaymentID { get; set; } public string ConfirmationUrl { get; set; } = ""; public string Message { get; set; } = ""; }
    public sealed class OnlinePaymentStatusResponse { public bool Status { get; set; } public string PaymentStatus { get; set; } = ""; public string Message { get; set; } = ""; }

    public class DeliveriesResponse
    {
        public bool Status { get; set; }
        public List<Delivery> Deliveries { get; set; } = new();
    }

    // ===================== СКЛАД =====================
    public class Ingredient
    {
        public int IngredientID { get; set; }
        public string Name { get; set; } = "";
        public decimal Stock { get; set; }
        public string Unit { get; set; } = "";
        public decimal PricePerUnit { get; set; }
    }

    public class IngredientsResponse
    {
        public bool Status { get; set; }
        public List<Ingredient> Ingredients { get; set; } = new();
    }

    public class TechCard
    {
        public int TechCardID { get; set; }
        public int MenuItemID { get; set; }
        public int IngredientID { get; set; }
        public decimal Quantity { get; set; }
        public MenuItem? MenuItem { get; set; }
        public Ingredient? Ingredient { get; set; }
    }

    public class TechCardsResponse
    {
        public bool Status { get; set; }
        public List<TechCard> TechCards { get; set; } = new();
    }

    // ===================== ОТЧЁТЫ =====================
    public class RevenueReport
    {
        public bool Status { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCost { get; set; }
        public decimal Profit { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageCheck { get; set; }
    }

    public class ShiftRevenue
    {
        public bool Status { get; set; }
        public DateTime ShiftStart { get; set; }
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageCheck { get; set; }
    }

    public class PopularDish
    {
        public int MenuItemID { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public int TotalOrdered { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PopularDishesResponse
    {
        public bool Status { get; set; }
        public List<PopularDish> PopularDishes { get; set; } = new();
    }

    public class StockReport
    {
        public bool Status { get; set; }
        public List<Ingredient> Ingredients { get; set; } = new();
        public int LowStockCount { get; set; }
        public List<Ingredient> LowStockItems { get; set; } = new();
    }

    public class DishCost
    {
        public int MenuItemID { get; set; }
        public string DishName { get; set; } = "";
        public decimal SellingPrice { get; set; }
        public decimal Cost { get; set; }
        public decimal Profit { get; set; }
        public double Margin { get; set; }
    }

    public class CostReportResponse
    {
        public bool Status { get; set; }
        public List<DishCost> Dishes { get; set; } = new();
    }

    public class HourlyLoad
    {
        public int Hour { get; set; }
        public int OrderCount { get; set; }
        public int ItemCount { get; set; }
    }

    public class KitchenStatusStats
    {
        public int Total { get; set; }
        public int NewCount { get; set; }
        public int PreparingCount { get; set; }
        public int ReadyCount { get; set; }
        public int PaidCount { get; set; }
        public int CancelledCount { get; set; }
    }

    public class KitchenLoadReport
    {
        public bool Status { get; set; }
        public DateTime Date { get; set; }
        public List<HourlyLoad> ByHour { get; set; } = new();
        public KitchenStatusStats StatusStats { get; set; } = new();
    }

    public class ReservationSummary
    {
        public int ReservationID { get; set; }
        public int? TableNumber { get; set; }
        public string GuestName { get; set; } = "";
        public int Persons { get; set; }
        public string Time { get; set; } = "";
    }

    public class HallLoadReport
    {
        public bool Status { get; set; }
        public int TotalTables { get; set; }
        public int OccupiedTables { get; set; }
        public int ReservedTables { get; set; }
        public int FreeTables { get; set; }
        public double OccupancyRate { get; set; }
        public int DineInOrders { get; set; }
        public List<ReservationSummary> ReservationsToday { get; set; } = new();
    }

    public class SupportConversationDto { public int ConversationID { get; set; } public int UserID { get; set; } public string Status { get; set; } = ""; public DateTime CreatedAt { get; set; } public DateTime UpdatedAt { get; set; } public int TicketID { get; set; } public string Subject { get; set; } = ""; }
    public class SupportMessageDto { public int MessageID { get; set; } public int ConversationID { get; set; } public string Sender { get; set; } = ""; public string Content { get; set; } = ""; public DateTime CreatedAt { get; set; } }
    public class SupportItemsResponse<T> { public bool Status { get; set; } public List<T> Items { get; set; } = new(); }
    public class SupportMessagesResponse { public bool Status { get; set; } public List<SupportMessageDto> Messages { get; set; } = new(); }
    public class SupportChatResponse { public bool Status { get; set; } public int ConversationID { get; set; } public string Message { get; set; } = ""; }
    public class SupplyRequestDto { public int SupplyRequestID { get; set; } public int IngredientID { get; set; } public string IngredientName { get; set; } = ""; public decimal Quantity { get; set; } public string Supplier { get; set; } = ""; public int? SupplierUserID { get; set; } public string SupplierUserName { get; set; } = ""; public string Status { get; set; } = ""; public DateTime CreatedAt { get; set; } public DateTime? ReceivedAt { get; set; } }
    public class SupportTicketDto { public int TicketID { get; set; } public int ConversationID { get; set; } public int UserID { get; set; } public string Subject { get; set; } = ""; public string Status { get; set; } = ""; public DateTime CreatedAt { get; set; } }
    public class PaymentRequestDto { public int PaymentTransactionID { get; set; } public int OrderID { get; set; } public decimal Amount { get; set; } public string Method { get; set; } = ""; public DateTime CreatedAt { get; set; } }
    public class StocktakeDto { public int StocktakeID { get; set; } public DateTime CreatedAt { get; set; } public string Notes { get; set; } = ""; public List<StocktakeLineDto> Items { get; set; } = new(); }
    public class StocktakeLineDto { public int IngredientID { get; set; } public decimal ExpectedStock { get; set; } public decimal CountedStock { get; set; } }
    public class StaffPerformanceResponse { public bool Status { get; set; } public List<WaiterPerformanceDto> Waiters { get; set; } = new(); public List<CookPerformanceDto> Cooks { get; set; } = new(); public List<CourierPerformanceDto> Couriers { get; set; } = new(); }
    public class WaiterPerformanceDto { public int UserID { get; set; } public string UserName { get; set; } = ""; public int OrderCount { get; set; } public decimal Revenue { get; set; } public decimal AverageCheck { get; set; } }
    public class CookPerformanceDto { public int UserID { get; set; } public string UserName { get; set; } = ""; public int CompletedOrders { get; set; } public double AveragePreparationMinutes { get; set; } }
    public class CourierPerformanceDto { public int? UserID { get; set; } public string UserName { get; set; } = ""; public int AssignedCount { get; set; } public int DeliveredCount { get; set; } }
}
