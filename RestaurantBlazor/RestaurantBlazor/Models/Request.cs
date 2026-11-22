namespace RestaurantBlazor.Models
{
    public class CreateTableRequest
    {
        public int Number { get; set; }
        public int Capacity { get; set; } = 4;
        public int PositionX { get; set; } = 1;
        public int PositionY { get; set; } = 1;
    }

    public class AuthRequest
    {
        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class RegisterRequest
    {
        public string Login { get; set; } = "";
        public string Password { get; set; } = "";
        public string UserName { get; set; } = "";
        public int Role { get; set; } = 4;
        public DateTime? BirthDate { get; set; }
        public int BranchID { get; set; } = 1;
    }

    public class CreateMenuItemRequest
    {
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Category { get; set; } = "";
        public int? MenuCategoryID { get; set; }
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
    }

    public class OrderItemRequest
    {
        public int MenuItemID { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderRequest
    {
        public int TableID { get; set; }
        public int WaiterID { get; set; }
        public string Type { get; set; } = "DineIn";
        public string PickupAddress { get; set; } = "";
        public List<OrderItemRequest> Items { get; set; } = new();
        public int Priority { get; set; }
        public int? BranchID { get; set; }
    }

    public class UpdateStatusRequest
    {
        public int OrderID { get; set; }
        public string Status { get; set; } = "";
    }

    public class ReservationRequest
    {
        public int TableID { get; set; }
        public string GuestName { get; set; } = "";
        public string GuestPhone { get; set; } = "";
        public DateTime DateTime { get; set; }
        public int Persons { get; set; }
    }

    public class CreateDeliveryRequest
    {
        public int OrderID { get; set; }
        public string ClientPhone { get; set; } = "";
        public string Address { get; set; } = "";
    }

    public class AssignCourierRequest
    {
        public int DeliveryID { get; set; }
        public int CourierID { get; set; }
    }

    public class UpdateDeliveryStatusRequest
    {
        public int DeliveryID { get; set; }
        public string Status { get; set; } = "";
        public int? EstimatedMinutes { get; set; }
    }

    public class CreateIngredientRequest
    {
        public string Name { get; set; } = "";
        public decimal Stock { get; set; }
        public string Unit { get; set; } = "";
        public decimal PricePerUnit { get; set; }
    }

    public class AddStockRequest
    {
        public int IngredientID { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateTechCardRequest
    {
        public int MenuItemID { get; set; }
        public int IngredientID { get; set; }
        public decimal Quantity { get; set; }
    }

    public class SupportChatRequest { public int? ConversationID { get; set; } public string Message { get; set; } = ""; }
    public class SupportReplyRequest { public string Message { get; set; } = ""; public bool Resolve { get; set; } }
    public class SupplyRequestCreate { public int IngredientID { get; set; } public decimal Quantity { get; set; } public int SupplierUserID { get; set; } }
    public class StocktakeCountRequest { public int IngredientID { get; set; } public decimal CountedStock { get; set; } }
    public class StocktakeRequest { public string Notes { get; set; } = ""; public List<StocktakeCountRequest> Items { get; set; } = new(); }
    public class RoutePlanRequest { public List<int> DeliveryIDs { get; set; } = new(); }
    public class StartPaymentRequest { public int OrderID { get; set; } public string Method { get; set; } = "Cash"; }
}
