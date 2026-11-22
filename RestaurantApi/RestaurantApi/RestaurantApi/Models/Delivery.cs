using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class Delivery
    {
        [Key]
        public int DeliveryID { get; set; }
        public int OrderID { get; set; }
        public int? CourierID { get; set; }
        public string ClientPhone { get; set; }
        public string Address { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Assigned, Accepted, InTransit, Delivered
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EstimatedArrival { get; set; } // расчётное время прибытия
        public int RouteSequence { get; set; }
        [ForeignKey("OrderID")]
        public Order Order { get; set; }
        [ForeignKey("CourierID")]
        public User Courier { get; set; }
    }
}
