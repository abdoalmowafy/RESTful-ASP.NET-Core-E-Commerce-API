using ECommerce.Authentication.Contracts;
using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Authentication.Services;

public interface IAuthRegistrationService
{
    Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}

public class AuthRegistrationService(UserManager<ApplicationUser> userManager) : IAuthRegistrationService
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    public async Task<Result> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Result.Failure(UserErrors.EmailDuplicated);

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber)
            && _userManager.Users.Any(u => u.PhoneNumber == request.PhoneNumber))
            return Result.Failure(UserErrors.PhoneDuplicated);

        var user = new ApplicationUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            UserName = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            EmailConfirmed = false,
            PhoneNumberConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var error = result.Errors.First();
            return Result.Failure(new Error(error.Code, error.Description, StatusCodes.Status400BadRequest));
        }

        await _userManager.AddToRoleAsync(user, "Customer");

        user.Cart = new Cart { UserId = user.Id };
        user.CustomerProfile = new CustomerProfile { Id = user.Id, Status = ProfileStatus.Active };
        await _userManager.UpdateAsync(user);

        return Result.Succeed();
    }
}
