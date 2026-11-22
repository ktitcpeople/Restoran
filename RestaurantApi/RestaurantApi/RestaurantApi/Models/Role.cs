using System.ComponentModel.DataAnnotations;

namespace RestaurantApi.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        public string Name { get; set; }
    }
}
