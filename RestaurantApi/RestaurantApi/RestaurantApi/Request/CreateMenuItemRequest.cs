namespace RestaurantApi.Request
{
    public class CreateMenuItemRequest
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public int? MenuCategoryID { get; set; }
        public string KitchenStation { get; set; } = "Общая кухня";
        public string ImageUrl { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal Protein { get; set; }
        public decimal Fat { get; set; }
        public decimal Carbohydrates { get; set; }
        public decimal Calories { get; set; }
        public int WeightGrams { get; set; }
        public byte[]? ImageData { get; set; }
        public string ImageMimeType { get; set; } = "";
    }
}
