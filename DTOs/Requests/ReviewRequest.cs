using System.ComponentModel.DataAnnotations;

namespace ECommerceAPI.DTOs.Requests
{
    public class ReviewRequest
    {
        [Required] public required int ProductId { get; set; }
        [Required][Range(1, 5)] public required byte Rating { get; set; }
        [DataType(DataType.MultilineText)] public required string Text { get; set; }
    }
}
