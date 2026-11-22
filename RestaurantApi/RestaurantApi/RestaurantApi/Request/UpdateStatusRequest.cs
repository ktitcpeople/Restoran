namespace RestaurantApi.Request
{
    public class UpdateStatusRequest
    {
        public int OrderID { get; set; }
        public string Status { get; set; }
        public int ActorUserID { get; set; }
        public int ActorRole { get; set; }
    }
}
