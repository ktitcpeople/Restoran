using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class Ingredient
    {
        [Key]
        public int IngredientID { get; set; }
        public string Name { get; set; }
        public decimal Stock { get; set; }
        public string Unit { get; set; } // kg, l, pcs
        public decimal PricePerUnit { get; set; } = 0; // цена за единицу для расчёта себестоимости
        public int BranchID { get; set; } = 1;
    }
}
