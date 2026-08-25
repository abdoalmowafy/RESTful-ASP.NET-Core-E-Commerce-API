using ECommerce.Infrastructure.Entities;

namespace ECommerce.Authentication.Jwt;

public interface IJwtProvider
{
    Task<(string Token, int ExpiresIn)> GenerateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
