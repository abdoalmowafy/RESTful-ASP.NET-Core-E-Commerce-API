using Ordering.Management.Contracts;

namespace Ordering.Management.Services;

public interface IReturnManagementService
{
    Task<Result<PaginatedList<ManagementReturnResponse>>> GetAsync(ReturnStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> UpdateStatusAsync(int returnId, UpdateReturnStatusRequest request, CancellationToken cancellationToken = default);
    Task<Result> AssignTransporterAsync(int returnId, AssignTransporterRequest request, CancellationToken cancellationToken = default);
}

public class ReturnManagementService(AppDbContext context) : IReturnManagementService
{
    private static readonly Dictionary<ReturnStatus, ReturnStatus[]> AllowedTransitions = new()
    {
        [ReturnStatus.OnTheWay] = [ReturnStatus.Processing],
        [ReturnStatus.Returned] = [ReturnStatus.OnTheWay],
        [ReturnStatus.Cancelled] = [ReturnStatus.Processing]
    };

    private readonly AppDbContext _context = context;

    public async Task<Result<PaginatedList<ManagementReturnResponse>>> GetAsync(ReturnStatus? status, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.OrderProduct).ThenInclude(op => op!.Product)
            .Include(r => r.RequestedBy)
            .Include(r => r.Transporter)
            .Where(r => r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt);

        if (status.HasValue)
            query = (IOrderedQueryable<ReturnRequest>)query.Where(r => r.Status == status.Value);

        var page = await PaginatedList<ReturnRequest>.CreateAsync(query, pageIndex, pageSize, cancellationToken);
        var mapped = page.Items.Select(Map).ToList();

        return Result.Succeed(new PaginatedList<ManagementReturnResponse>(mapped, page.PageNumber, page.TotalCount, page.TotalPages));
    }

    public async Task<Result> UpdateStatusAsync(int returnId, UpdateReturnStatusRequest request, CancellationToken cancellationToken = default)
    {
        var returnRequest = await _context.ReturnRequests
            .Include(r => r.OrderProduct)
            .FirstOrDefaultAsync(r => r.Id == returnId && r.DeletedAt == null, cancellationToken);

        if (returnRequest is null)
            return Result.Failure(OrderingErrors.Return.NotFound);

        if (returnRequest.Status == request.Status)
            return Result.Succeed();

        if (!AllowedTransitions.TryGetValue(request.Status, out var allowedFrom) || !allowedFrom.Contains(returnRequest.Status))
            return Result.Failure(OrderingErrors.Order.InvalidStatusTransition);

        returnRequest.Status = request.Status;

        if (request.Status == ReturnStatus.Returned)
        {
            returnRequest.ReturnedAt = DateTime.UtcNow;
            if (returnRequest.OrderProduct is not null)
                returnRequest.OrderProduct.ReturnedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Succeed();
    }

    public async Task<Result> AssignTransporterAsync(int returnId, AssignTransporterRequest request, CancellationToken cancellationToken = default)
    {
        var returnRequest = await _context.ReturnRequests
            .FirstOrDefaultAsync(r => r.Id == returnId && r.DeletedAt == null, cancellationToken);

        if (returnRequest is null)
            return Result.Failure(OrderingErrors.Return.NotFound);

        var transporter = await _context.Users.FindAsync([request.TransporterId], cancellationToken);
        if (transporter is null)
            return Result.Failure(UserErrors.NotFound);

        if (!await _context.UserRoles
                .Join(_context.Roles,
                    ur => ur.RoleId,
                    r => r.Id,
                    (ur, r) => new { ur.UserId, RoleName = r.Name })
                .AnyAsync(x => x.UserId == transporter.Id && x.RoleName == DefaultRoles.Driver, cancellationToken))
            return Result.Failure(OrderingErrors.Return.NotTransporter);

        returnRequest.TransporterId = transporter.Id;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Succeed();
    }

    private static ManagementReturnResponse Map(ReturnRequest r)
        => new(
            r.Id,
            r.OrderId,
            r.OrderProduct?.Product?.Name ?? string.Empty,
            r.Quantity,
            r.Reason,
            r.Status,
            r.CreatedAt,
            $"{r.RequestedBy?.FirstName} {r.RequestedBy?.LastName}".Trim(),
            r.Transporter is null ? null : $"{r.Transporter.FirstName} {r.Transporter.LastName}".Trim());
}

public record UpdateReturnStatusRequest(ReturnStatus Status);
