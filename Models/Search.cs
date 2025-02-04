using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class Search
    {
        [Key] public int Id { get; set; }
        public User? User { get; set; }
        [Required] public required string KeyWord { get; set; }
        public Category? Category { get; set; }
    }
}
