using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.Models
{
    public class DeleteHistory
    {
        [Key] public int Id { get; set; }
        [Required] public required User Deleter { get; set; }
        [Required] public required string DeletedType { get; set; }
        [Required] public required int DeletedId { get; set; }
        [Required][DataType(DataType.DateTime)] public DateTime DeleteDateTime { get; set; } = DateTime.Now;
    }
}
