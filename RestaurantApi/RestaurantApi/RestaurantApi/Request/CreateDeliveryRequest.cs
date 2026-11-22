namespace RestaurantApi.Request
{
    public class CreateDeliveryRequest
    {
        public int OrderID { get; set; }
        public string ClientPhone { get; set; }
        public string Address { get; set; }
    }
}
