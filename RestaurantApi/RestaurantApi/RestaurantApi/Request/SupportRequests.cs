namespace RestaurantApi.Request;

public sealed class SupportChatRequest
{
    public int? ConversationID { get; set; }
    public string Message { get; set; } = "";
}

public sealed class SupportReplyRequest
{
    public string Message { get; set; } = "";
    public bool Resolve { get; set; }
}


public sealed class SupplyRequestCreate
{
    public int IngredientID { get; set; }
    public decimal Quantity { get; set; }
    public int SupplierUserID { get; set; }
}

public sealed class StocktakeRequest
{
    public string Notes { get; set; } = "";
    public List<StocktakeCountRequest> Items { get; set; } = new();
}

public sealed class StocktakeCountRequest
{
    public int IngredientID { get; set; }
    public decimal CountedStock { get; set; }
}

public sealed class RoutePlanRequest
{
    public List<int> DeliveryIDs { get; set; } = new();
}

public sealed class StartPaymentRequest
{
    public int OrderID { get; set; }
    public string Method { get; set; } = "Cash";
}
