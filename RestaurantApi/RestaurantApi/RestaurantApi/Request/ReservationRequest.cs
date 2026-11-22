namespace RestaurantApi.Request
{
    public class ReservationRequest
    {
        public int TableID { get; set; }
        public string GuestName { get; set; }
        public string GuestPhone { get; set; }
        public DateTime DateTime { get; set; }
        public int Persons { get; set; }
    }
}
