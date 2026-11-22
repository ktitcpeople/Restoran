namespace RestaurantApi.Request
{
    public class CreateTableRequest
    {
        public int Number { get; set; }
        public int Capacity { get; set; }
        public int PositionX { get; set; } = 1;
        public int PositionY { get; set; } = 1;
    }
}
