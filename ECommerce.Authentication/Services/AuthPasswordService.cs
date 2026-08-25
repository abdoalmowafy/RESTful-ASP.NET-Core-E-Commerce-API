using ECommerce.Authentication.Contracts;
using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;
using Microsoft.AspNetCore.Http;

namespace ECommerce.Authentication.Services;

public interface IAuthPasswordService
{
    Task<Result> ChangePasswordAsync(ClaimsPrincipal actor, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}

public class AuthPasswordService(UserManager<ApplicationUser> userManager) : IAuthPasswordService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result> ChangePasswordAsync(
        ClaimsPrincipal actor,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(actor.GetUserId());
        if (user is null)
            return Result.Failure(UserErrors.NotFound);

        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
            return Result.Failure(Error.BadRequest("Auth.SamePassword", "New password must differ from the current one"));

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        return Result.Succeed();
    }
}
