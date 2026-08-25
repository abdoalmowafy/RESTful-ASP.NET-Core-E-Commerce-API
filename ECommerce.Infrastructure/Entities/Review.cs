using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities.Enums;

namespace ECommerce.Infrastructure.Entities;

public class Review : IHasEditHistory
{
    public int Id { get; set; }
    public string ReviewerId { get; set; } = string.Empty;
    public ApplicationUser? Reviewer { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public byte Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    public ICollection<EditHistory> EditsHistory { get; set; } = [];
}
