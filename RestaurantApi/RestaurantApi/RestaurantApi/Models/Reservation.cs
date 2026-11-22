using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class Reservation
    {
        [Key] 
        public int ReservationID { get; set; }
        public int TableID { get; set; }
        public string GuestName { get; set; }
        public string GuestPhone { get; set; }
        public DateTime DateTime { get; set; }
        public int Persons { get; set; }
        public string Status { get; set; } = "Active"; // Active, Cancelled

        [ForeignKey("TableID")] 
        public RestaurantTable Table { get; set; }
    }
}
