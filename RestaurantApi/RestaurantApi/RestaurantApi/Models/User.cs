using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public string UserName { get; set; }
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        public Role Role { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string? Token { get; set; }
        public DateTime? BirthDate { get; set; }
        public int BranchID { get; set; } = 1;
        [ForeignKey(nameof(BranchID))]
        public Branch? Branch { get; set; }
    }
}
