using RestaurantApi.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class OrderItem
{
    [Key] 
    public int OrderItemID { get; set; }
    public int OrderID { get; set; }
    public int MenuItemID { get; set; }
    public int Quantity { get; set; }

    [ForeignKey("OrderID")] 
    public Order Order { get; set; }
    [ForeignKey("MenuItemID")] 
    public MenuItem MenuItem { get; set; }
}
