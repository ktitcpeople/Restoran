namespace RestaurantApi.Request
{
    public class CreateTechCardRequest
    {
        public int MenuItemID { get; set; }
        public int IngredientID { get; set; }
        public decimal Quantity { get; set; }
    }
}
