using ECommerce.Infrastructure.Abstractions;
using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECommerce.Authentication.Jwt;

/// <summary>
/// Issues access tokens carrying the user's CURRENT profile statuses as claims
/// (cliniq-style doctor_status/patient_status → here customer/seller/driver/store).
/// Claims refresh on next login or token rotation — status changes made after
/// issuance take effect once the client rotates its token.
/// </summary>
public class JwtProvider(
    IOptions<JwtOptions> options,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<(string Token, int ExpiresIn)> GenerateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("email_confirmed", user.EmailConfirmed.ToString().ToLowerInvariant()),
            new("phone_number_confirmed", user.PhoneNumberConfirmed.ToString().ToLowerInvariant())
        };

        foreach (var role in roles.Where(r => DefaultRoles.IsAdminRole(r)))
            claims.Add(new Claim("roles", role));

        var hasCustomerProfile = await _dbContext.CustomerProfiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == user.Id, cancellationToken);

        if (hasCustomerProfile)
        {
            var status = await _dbContext.CustomerProfiles
                .AsNoTracking()
                .Where(p => p.Id == user.Id)
                .Select(p => (RegistrationStatus?)p.RegistrationStatus)
                .FirstOrDefaultAsync(cancellationToken);

            claims.Add(new Claim("customer_status", (status ?? RegistrationStatus.PendingVerification).ToString()));
        }

        var hasStore = await _dbContext.Stores
            .AsNoTracking()
            .AnyAsync(s => s.OwnerId == user.Id && s.DeletedAt == null, cancellationToken);

        if (hasStore)
        {
            var storeStatus = await _dbContext.Stores
                .AsNoTracking()
                .Where(s => s.OwnerId == user.Id && s.DeletedAt == null)
                .Select(s => (StoreStatus?)s.Status)
                .FirstOrDefaultAsync(cancellationToken);

            claims.Add(new Claim("store_status",
                (storeStatus ?? StoreStatus.PendingVerification).ToString()));
        }

        var hasDriverProfile = await _dbContext.DriverProfiles
            .AsNoTracking()
            .AnyAsync(p => p.Id == user.Id, cancellationToken);

        if (hasDriverProfile)
        {
            var driverStatus = await _dbContext.DriverProfiles
                .AsNoTracking()
                .Where(p => p.Id == user.Id)
                .Select(p => (RegistrationStatus?)p.RegistrationStatus)
                .FirstOrDefaultAsync(cancellationToken);

            claims.Add(new Claim("driver_status",
                (driverStatus ?? RegistrationStatus.PendingVerification).ToString()));
        }

        var symmetricSecurityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var signingCredentials = new SigningCredentials(symmetricSecurityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes),
            signingCredentials: signingCredentials);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, _options.ExpiryMinutes * 60);
    }
}