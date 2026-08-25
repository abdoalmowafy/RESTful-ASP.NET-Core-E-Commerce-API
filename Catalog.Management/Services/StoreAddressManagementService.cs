using Catalog.Management.Contracts;

namespace Catalog.Management.Services;

public interface IStoreAddressManagementService
{
    Task<Result<IReadOnlyList<AddressManagementResponse>>> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<AddressManagementResponse>> CreateAsync(StoreAddressRequest request, CancellationToken cancellationToken = default);
    Task<Result<AddressManagementResponse>> UpdateAsync(int id, StoreAddressRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class StoreAddressManagementService(AppDbContext context) : IStoreAddressManagementService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<IReadOnlyList<AddressManagementResponse>>> GetAsync(CancellationToken cancellationToken = default)
    {
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == null && a.DeletedAt == null)
            .OrderBy(a => a.Id)
            .Select(a => ToResponse(a))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<AddressManagementResponse>>(addresses);
    }

    public async Task<Result<AddressManagementResponse>> CreateAsync(StoreAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = new Address
        {
            Apartment = request.Apartment,
            Floor = request.Floor,
            Building = request.Building,
            Street = request.Street,
            City = request.City,
            State = request.State,
            Country = request.Country,
            PostalCode = request.PostalCode
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(ToResponse(address));
    }

    public async Task<Result<AddressManagementResponse>> UpdateAsync(int id, StoreAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await FindAsync(id, cancellationToken);
        if (address is null)
            return Result.Failure<AddressManagementResponse>(CatalogErrors.StoreAddress.NotFound);

        address.Apartment = request.Apartment;
        address.Floor = request.Floor;
        address.Building = request.Building;
        address.Street = request.Street;
        address.City = request.City;
        address.State = request.State;
        address.Country = request.Country;
        address.PostalCode = request.PostalCode;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(ToResponse(address));
    }

    public async Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var address = await FindAsync(id, cancellationToken);
        if (address is null)
            return Result.Failure(CatalogErrors.StoreAddress.NotFound);

        address.DeletedAt = DateTime.UtcNow;

        _context.DeletesHistory.Add(new DeleteHistory
        {
            DeleterId = actor.GetUserId(),
            EntityType = nameof(Address),
            EntityId = address.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed();
    }

    private async Task<Address?> FindAsync(int id, CancellationToken cancellationToken)
        => await _context.Addresses
            .Where(a => a.UserId == null && a.DeletedAt == null)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    private static AddressManagementResponse ToResponse(Address a)
        => new(a.Id, a.Apartment, a.Floor, a.Building, a.Street, a.City, a.State, a.Country, a.PostalCode);
}
