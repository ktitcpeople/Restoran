using System.ComponentModel.DataAnnotations;

namespace RestaurantApi.Models;

public sealed class Branch
{
    [Key]
    public int BranchID { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public bool IsActive { get; set; } = true;
}
