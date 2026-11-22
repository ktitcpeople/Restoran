using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        public int? TableID { get; set; }
        public int WaiterID { get; set; }
        public string Status { get; set; } = "New";
        public string Type { get; set; } = "DineIn";
        public string PickupAddress { get; set; } = "ул. Баумана, 54, Казань";
        public int Priority { get; set; } = 0; // 0 - обычный, 1 - высокий
        public int? PreparingByUserID { get; set; }
        public int? ReadyByUserID { get; set; }
        public DateTime? PreparingAt { get; set; }
        public DateTime? ReadyAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalPrice { get; set; }
        public int? CustomerUserID { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal DiscountPercent { get; set; }
        public string DiscountReason { get; set; } = "";
        public int? BirthdayDiscountYear { get; set; }
        public int BranchID { get; set; } = 1;

        [ForeignKey("TableID")]
        public RestaurantTable Table { get; set; }
        public List<OrderItem> Items { get; set; } = new();
    }
}
