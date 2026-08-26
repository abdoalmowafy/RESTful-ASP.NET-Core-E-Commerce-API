using Admin.Management.Contracts;

namespace Admin.Management.Services;

public interface IAdminAccountsService
{
    Task<Result<PaginatedList<AdminResponse>>> GetAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<Result> CreateAsync(CreateAdminRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetStatusAsync(string adminId, UpdateAdminStatusRequest request, CancellationToken cancellationToken = default);
}

public class AdminAccountsService(UserManager<ApplicationUser> userManager) : IAdminAccountsService
{
    private static readonly string[] StaffRoles = ["SuperAdmin", "Admin"];

    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result<PaginatedList<AdminResponse>>> GetAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 50);

        var allUsers = await _userManager.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        var admins = new List<AdminResponse>();
        foreach (var user in allUsers)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any(r => StaffRoles.Contains(r)))
                continue;

            admins.Add(new AdminResponse(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email!,
                user.IsDisabled,
                [.. roles]));
        }

        var pageItems = admins
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return Result.Succeed(new PaginatedList<AdminResponse>(pageItems, pageIndex, admins.Count, pageSize));
    }

    public async Task<Result> CreateAsync(CreateAdminRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Result.Failure(UserErrors.EmailDuplicated);

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            EmailConfirmed = true,
            PhoneNumberConfirmed = true,
            AdminProfile = new AdminProfile { Id = Guid.NewGuid().ToString() }
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        user.AdminProfile = new AdminProfile { Id = user.Id };
        await _userManager.UpdateAsync(user);

        await _userManager.AddToRoleAsync(user, "Admin");
        return Result.Succeed();
    }

    public async Task<Result> SetStatusAsync(string adminId, UpdateAdminStatusRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(adminId);
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Any(r => StaffRoles.Contains(r)))
            return Result.Failure(UserErrors.NotFound);

        if (roles.Contains("SuperAdmin"))
            return Result.Failure(Error.Forbidden("Admins.SuperAdminImmutable", "Super administrators cannot be disabled"));

        user.IsDisabled = request.Disabled;
        await _userManager.UpdateAsync(user);

        return Result.Succeed();
    }
}
