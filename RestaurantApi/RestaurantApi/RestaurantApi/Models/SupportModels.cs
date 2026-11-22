using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models;

public class SupportConversation
{
    [Key] public int ConversationID { get; set; }
    public int UserID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Open";
    public int BranchID { get; set; } = 1;
    public List<SupportMessage> Messages { get; set; } = new();
}

public class SupportMessage
{
    [Key] public int MessageID { get; set; }
    public int ConversationID { get; set; }
    [ForeignKey(nameof(ConversationID))] public SupportConversation? Conversation { get; set; }
    public int? UserID { get; set; }
    public string Sender { get; set; } = "user"; // user, operator
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class SupportTicket
{
    [Key] public int TicketID { get; set; }
    public int ConversationID { get; set; }
    [ForeignKey(nameof(ConversationID))] public SupportConversation? Conversation { get; set; }
    public int UserID { get; set; }
    public int? OperatorID { get; set; }
    public string Status { get; set; } = "Open";
    public string Subject { get; set; } = "Обращение в поддержку";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}

public class KnowledgeArticle
{
    [Key] public int ArticleID { get; set; }
    public int? CreatedByUserID { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public string Keywords { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class SupplyRequest
{
    [Key] public int SupplyRequestID { get; set; }
    public int IngredientID { get; set; }
    [ForeignKey(nameof(IngredientID))] public Ingredient? Ingredient { get; set; }
    public decimal Quantity { get; set; }
    public string Supplier { get; set; } = "";
    public int? SupplierUserID { get; set; }
    [ForeignKey(nameof(SupplierUserID))] public User? SupplierUser { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReceivedAt { get; set; }
}

public class Stocktake
{
    [Key] public int StocktakeID { get; set; }
    public int CreatedByUserID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Notes { get; set; } = "";
    public int BranchID { get; set; } = 1;
    public List<StocktakeItem> Items { get; set; } = new();
}

public class StocktakeItem
{
    [Key] public int StocktakeItemID { get; set; }
    public int StocktakeID { get; set; }
    [ForeignKey(nameof(StocktakeID))] public Stocktake? Stocktake { get; set; }
    public int IngredientID { get; set; }
    public decimal ExpectedStock { get; set; }
    public decimal CountedStock { get; set; }
}

public class StockTransaction
{
    [Key] public int StockTransactionID { get; set; }
    public int IngredientID { get; set; }
    [ForeignKey(nameof(IngredientID))] public Ingredient? Ingredient { get; set; }
    public int? UserID { get; set; }
    public decimal QuantityDelta { get; set; }
    public string Reason { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PaymentTransaction
{
    [Key] public int PaymentTransactionID { get; set; }
    public int OrderID { get; set; }
    [ForeignKey(nameof(OrderID))] public Order? Order { get; set; }
    public int UserID { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public string Status { get; set; } = "Pending";
    public string? ProviderPaymentID { get; set; }
    public string? ConfirmationUrl { get; set; }
    public string? IdempotenceKey { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
}
