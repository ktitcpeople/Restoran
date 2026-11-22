namespace RestaurantApi.Request
{
    public class CreateOrderRequest
    {
        public int TableID { get; set; }
        public int WaiterID { get; set; }
        public string Type { get; set; } = "DineIn"; // DineIn, Delivery, Takeaway
        public string PickupAddress { get; set; } = "";
        public int Priority { get; set; }
        public List<OrderItemRequest> Items { get; set; }
        public int? CustomerUserID { get; set; }
        public int? BranchID { get; set; }
    }
}
