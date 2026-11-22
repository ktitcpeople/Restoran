using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class MenuItem
    {
        [Key]
        public int MenuItemID { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public string KitchenStation { get; set; } = "Общая кухня";
        public string ImageUrl { get; set; } = "";
        public byte[]? ImageData { get; set; }
        public string ImageMimeType { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Calories { get; set; }
        public int WeightGrams { get; set; }
        public int BranchID { get; set; } = 1;

        public int? MenuCategoryID { get; set; }
        [ForeignKey(nameof(MenuCategoryID))]
        public MenuCategory? MenuCategory { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}
