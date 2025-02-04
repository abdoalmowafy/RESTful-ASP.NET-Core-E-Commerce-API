using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class Category
    {
        [Key] public int Id { get; set; }
        [Required] public required string Name { get; set; }
        public ICollection<Product> Products { get; set; } = [];
    }
}
