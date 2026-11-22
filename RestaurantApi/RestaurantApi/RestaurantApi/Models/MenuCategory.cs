using System.ComponentModel.DataAnnotations;

namespace RestaurantApi.Models
{
    public class MenuCategory
    {
        [Key]
        public int MenuCategoryID { get; set; }
        public string Name { get; set; } = "";
        public int BranchID { get; set; } = 1;
    }
}
