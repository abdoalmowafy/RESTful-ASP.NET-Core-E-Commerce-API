using ECommerce.Infrastructure.Entities;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Authentication.Jwt;

public interface IJwtProvider
{
    Task<(string Token, int ExpiresIn)> GenerateTokenAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
