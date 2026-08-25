using Catalog.Management.Contracts;

namespace Catalog.Management.Services;

public interface IPromoCodeManagementService
{
    Task<Result<IReadOnlyList<PromoCodeManagementResponse>>> GetAsync(CancellationToken cancellationToken = default);
    Task<Result<PromoCodeManagementResponse>> CreateAsync(PromoCodeRequest request, CancellationToken cancellationToken = default);
    Task<Result<PromoCodeManagementResponse>> UpdateAsync(int id, PromoCodeRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetActiveAsync(int id, StatusRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default);
}

public class PromoCodeManagementService(AppDbContext context) : IPromoCodeManagementService
{
    private readonly AppDbContext _context = context;

    public async Task<Result<IReadOnlyList<PromoCodeManagementResponse>>> GetAsync(CancellationToken cancellationToken = default)
    {
        var promoCodes = await _context.PromoCodes
            .AsNoTracking()
            .OrderByDescending(pc => pc.CreatedAt)
            .Select(pc => ToResponse(pc))
            .ToListAsync(cancellationToken);

        return Result.Succeed<IReadOnlyList<PromoCodeManagementResponse>>(promoCodes);
    }

    public async Task<Result<PromoCodeManagementResponse>> CreateAsync(PromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.PromoCodes.AnyAsync(pc => pc.Code == request.Code, cancellationToken))
            return Result.Failure<PromoCodeManagementResponse>(CatalogErrors.PromoCode.CodeDuplicated);

        var promoCode = new PromoCode
        {
            Code = request.Code.ToUpperInvariant(),
            Description = request.Description,
            Percent = request.Percent,
            MaxSaleCents = request.MaxSaleCents,
            Active = request.Active
        };

        _context.PromoCodes.Add(promoCode);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(ToResponse(promoCode));
    }

    public async Task<Result<PromoCodeManagementResponse>> UpdateAsync(int id, PromoCodeRequest request, CancellationToken cancellationToken = default)
    {
        var promoCode = await _context.PromoCodes.FindAsync([id], cancellationToken);
        if (promoCode is null || promoCode.DeletedAt is not null)
            return Result.Failure<PromoCodeManagementResponse>(CatalogErrors.PromoCode.NotFound);

        if (await _context.PromoCodes.AnyAsync(pc => pc.Code == request.Code && pc.Id != id, cancellationToken))
            return Result.Failure<PromoCodeManagementResponse>(CatalogErrors.PromoCode.CodeDuplicated);

        promoCode.Code = request.Code.ToUpperInvariant();
        promoCode.Description = request.Description;
        promoCode.Percent = request.Percent;
        promoCode.MaxSaleCents = request.MaxSaleCents;
        promoCode.Active = request.Active;

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed(ToResponse(promoCode));
    }

    public async Task<Result> SetActiveAsync(int id, StatusRequest request, CancellationToken cancellationToken = default)
    {
        var promoCode = await _context.PromoCodes.FindAsync([id], cancellationToken);
        if (promoCode is null || promoCode.DeletedAt is not null)
            return Result.Failure(CatalogErrors.PromoCode.NotFound);

        promoCode.Active = request.Active;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed();
    }

    public async Task<Result> DeleteAsync(int id, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        var promoCode = await _context.PromoCodes.FindAsync([id], cancellationToken);
        if (promoCode is null || promoCode.DeletedAt is not null)
            return Result.Failure(CatalogErrors.PromoCode.NotFound);

        promoCode.Active = false;
        promoCode.DeletedAt = DateTime.UtcNow;

        _context.DeletesHistory.Add(new DeleteHistory
        {
            DeleterId = actor.GetUserId(),
            EntityType = nameof(PromoCode),
            EntityId = promoCode.Id
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed();
    }

    private static PromoCodeManagementResponse ToResponse(PromoCode pc)
        => new(pc.Id, pc.Code, pc.Description, pc.Percent, pc.MaxSaleCents, pc.Active);
}
