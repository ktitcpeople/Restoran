using System.ComponentModel.DataAnnotations;

namespace RestaurantApi.Models
{
    public class RestaurantTable
    {
        [Key] 
        public int TableID { get; set; }
        public int Number { get; set; }
        public int Capacity { get; set; }
        public int PositionX { get; set; } = 1;
        public int PositionY { get; set; } = 1;
        public string Status { get; set; } = "Free";
        public int BranchID { get; set; } = 1;
    }
}
