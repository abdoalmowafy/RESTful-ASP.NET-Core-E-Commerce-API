namespace ECommerce.Infrastructure.Abstractions;

public interface IHasConcurrencyToken
{
    uint RowVersion { get; }
}
