namespace RestaurantApi.Request
{
    public class CreateIngredientRequest
    {
        public string Name { get; set; }
        public decimal Stock { get; set; }
        public string Unit { get; set; }
        public decimal PricePerUnit { get; set; } = 0;
    }
}
