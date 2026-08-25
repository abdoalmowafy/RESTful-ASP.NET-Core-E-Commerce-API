using ECommerce.Infrastructure.Entities;
using Microsoft.Extensions.Options;

namespace ECommerce.Authentication.Jwt;

public class JwtProvider(IOptions<JwtOptions> options, UserManager<ApplicationUser> userManager) : IJwtProvider
{
    private readonly JwtOptions _options = options.Value;
    private readonly UserManager<ApplicationUser> _userManager = userManager;

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

        foreach (var role in roles)
            claims.Add(new Claim("roles", role));

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
