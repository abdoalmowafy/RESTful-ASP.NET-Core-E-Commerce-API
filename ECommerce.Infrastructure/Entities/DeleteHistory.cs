namespace ECommerce.Infrastructure.Entities;

public class DeleteHistory
{
    public int Id { get; set; }
    public string DeleterId { get; set; } = string.Empty;
    public ApplicationUser? Deleter { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
}
