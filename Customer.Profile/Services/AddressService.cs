using Customer.Profile.Contracts;

namespace Customer.Profile.Services;

public interface IAddressService
{
    Task<Result<IReadOnlyList<CustomerAddressResponse>>> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<Result<CustomerAddressResponse>> CreateAsync(string userId, CreateAddressRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(string userId, int addressId, CancellationToken cancellationToken = default);
}

public class AddressService(AppDbContext context) : IAddressService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<IReadOnlyList<CustomerAddressResponse>>> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId && a.DeletedAt == null)
            .OrderBy(a => a.Id)
            .Select(ToResponse())
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<CustomerAddressResponse>>(addresses);
    }

    public async Task<Result<CustomerAddressResponse>> CreateAsync(string userId, CreateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!userExists)
            return Result.Failure<CustomerAddressResponse>(UserErrors.NotFound);

        var address = new Address
        {
            UserId = userId,
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

        return Result.Succeed(new CustomerAddressResponse(
            address.Id,
            address.Apartment,
            address.Floor,
            address.Building,
            address.Street,
            address.City,
            address.State,
            address.Country,
            address.PostalCode));
    }

    public async Task<Result> DeleteAsync(string userId, int addressId, CancellationToken cancellationToken = default)
    {
        var updated = await _context.Addresses
            .Where(a => a.Id == addressId && a.UserId == userId && a.DeletedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.DeletedAt, DateTime.UtcNow), cancellationToken);

        return updated == 0 ? Result.Failure(CatalogErrors.StoreAddress.NotFound) : Result.Succeed();
    }

    private static System.Linq.Expressions.Expression<Func<Address, CustomerAddressResponse>> ToResponse()
        => a => new CustomerAddressResponse(
            a.Id,
            a.Apartment,
            a.Floor,
            a.Building,
            a.Street,
            a.City,
            a.State,
            a.Country,
            a.PostalCode);
}
