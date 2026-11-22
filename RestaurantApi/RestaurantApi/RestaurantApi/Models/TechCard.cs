using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class TechCard
    {
        [Key]
        public int TechCardID { get; set; }
        public int MenuItemID { get; set; }
        public int IngredientID { get; set; }
        public decimal Quantity { get; set; }

        [ForeignKey("MenuItemID")] 
        public MenuItem MenuItem { get; set; }
        [ForeignKey("IngredientID")]
        public Ingredient Ingredient { get; set; }
    }

}
