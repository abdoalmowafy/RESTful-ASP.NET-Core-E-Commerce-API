using ECommerce.Authentication.Authorization;
using ECommerce.Authentication.Jwt;
using ECommerce.Authentication.Services;
using ECommerce.Infrastructure.Entities.Enums;
using ECommerce.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace ECommerce.Authentication;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthenticationModule(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing");

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = !environment.IsDevelopment(),
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken) &&
                        context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

services.AddAuthorization(options =>
        {
            options.AddPolicy(PolicyNames.VerifiedUser, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new VerifiedUserRequirement()));

            options.AddPolicy(PolicyNames.ActiveCustomer, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ProfileStatusRequirement(ProfileClaims.CustomerStatus,
                          RegistrationStatus.Active.ToString())));

            options.AddPolicy(PolicyNames.ActiveSeller, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ProfileStatusRequirement(ProfileClaims.StoreStatus,
                          StoreStatus.Active.ToString())));

            options.AddPolicy(PolicyNames.PendingDriver, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ProfileStatusRequirement(ProfileClaims.DriverStatus,
                          RegistrationStatus.PendingVerification.ToString(), RegistrationStatus.Rejected.ToString())));

            options.AddPolicy(PolicyNames.ActiveDriver, policy =>
                policy.RequireAuthenticatedUser()
                      .AddRequirements(new ProfileStatusRequirement(ProfileClaims.DriverStatus,
                          RegistrationStatus.Active.ToString())));
        });

        services.AddScoped<IAuthorizationHandler, VerifiedUserHandler>();
        services.AddScoped<IAuthorizationHandler, ProfileStatusHandler>();

        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthPermissionService, AuthPermissionService>();

        services.AddScoped<IJwtProvider, JwtProvider>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IAuthRegistrationService, AuthRegistrationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthSessionService, AuthSessionService>();
        services.AddScoped<IAuthProfileService, AuthProfileService>();
        services.AddScoped<IAuthPasswordService, AuthPasswordService>();
        services.AddScoped<IAccountVerificationService, AccountVerificationService>();

        services.Configure<Jwt.RefreshTokenOptions>(configuration.GetSection(Jwt.RefreshTokenOptions.SectionName));

        services
            .AddFluentValidationAutoValidation()
            .AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}
