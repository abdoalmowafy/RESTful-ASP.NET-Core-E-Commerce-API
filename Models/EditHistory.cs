using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class EditHistory
    {
        [Key] public int Id { get; set; }
        public User? Editor { get; set; }
        [Required] public required string EditedType { get; set; }
        [Required] public required string EditedId { get; set; }
        [Required] public required string EditedField { get; set; }
        [Required] public required string OldData { get; set; }
        [Required] public required string NewData { get; set; }
        [Required][DataType(DataType.DateTime)] public DateTime EditDateTime { get; set; } = DateTime.Now;
    }
}
