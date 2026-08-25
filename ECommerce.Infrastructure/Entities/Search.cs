namespace ECommerce.Infrastructure.Entities;

public class Search
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    public string KeyWord { get; set; } = string.Empty;
    public DateTime SearchedAt { get; set; } = DateTime.UtcNow;
}
