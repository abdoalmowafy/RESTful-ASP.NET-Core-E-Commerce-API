namespace ECommerce.Infrastructure.Entities;

public class EditHistory
{
    public int Id { get; set; }
    public string? EditorId { get; set; }
    public ApplicationUser? Editor { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
    public DateTime EditedAt { get; set; } = DateTime.UtcNow;
}
