using ECommerce.Infrastructure.Entities;

namespace ECommerce.Infrastructure.Abstractions;

public interface IHasEditHistory
{
    ICollection<EditHistory> EditsHistory { get; }
}
