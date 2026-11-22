namespace RestaurantApi.Request
{
    public class UpdateDeliveryStatusRequest
    {
        public int DeliveryID { get; set; }
        public string Status { get; set; }
        public int? EstimatedMinutes { get; set; } // сколько минут до прибытия (для InTransit)
    }
}
