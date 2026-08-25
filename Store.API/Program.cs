using Admin.Management;
using Admin.Profile;
using Catalog.Management;
using Catalog.Public;
using ECommerce.Authentication;
using ECommerce.Infrastructure;
using ECommerce.Infrastructure.Hubs;
using ECommerce.Infrastructure.Persistence;
using Customer.Management;
using Customer.Profile;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using System.Threading.RateLimiting;
using Ordering.Customer;
using Ordering.Management;
using Notifications;
using Roles.Management;
using Scalar.AspNetCore;
using Serilog;
using Seller.Management;
using Seller.Profile;
using Driver.Management;
using Driver.Profile;
using Shopping.Customer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Behind a trusted reverse proxy only. Cleared so container/proxy IPs are honoured in staging too.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Fail fast on missing production configuration
if (!builder.Environment.IsDevelopment())
{
    var failures = new List<string>();

    var jwtKey = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
        failures.Add("Jwt:Key must be set to at least 32 characters");

    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
        failures.Add("ConnectionStrings:DefaultConnection is required");

    if (failures.Count > 0)
        throw new InvalidOperationException(
            "Startup configuration invalid:" + Environment.NewLine + string.Join(Environment.NewLine, failures));
}

builder.Services.AddControllers()
    .AddApplicationPart(typeof(ECommerce.Authentication.Controllers.AuthenticationController).Assembly)
    .AddApplicationPart(typeof(Catalog.Public.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Catalog.Management.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Shopping.Customer.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Ordering.Customer.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Ordering.Management.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Admin.Management.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Admin.Profile.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Customer.Management.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Customer.Profile.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Seller.Management.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Seller.Profile.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Driver.Management.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Driver.Profile.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Roles.Management.DependencyInjection).Assembly)
    .AddApplicationPart(typeof(Notifications.DependencyInjection).Assembly);

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
var allowAllOrigins = builder.Environment.IsDevelopment() || allowedOrigins.Contains("*");

builder.Services.AddCors(options =>
{
    options.AddPolicy("StoreCorsPolicy", policy =>
    {
        if (allowAllOrigins)
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                  .SetIsOriginAllowedToAllowWildcardSubdomains()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        // Production with no configured origins => cross-origin browser calls are denied.
        // Mobile apps and server-to-server clients are unaffected by CORS.
    });
});

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "E-Commerce Store API";
        document.Info.Version = "v1";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT Bearer token"
        };

        var originalPaths = document.Paths.ToList();
        document.Paths.Clear();
        foreach (var path in originalPaths)
            document.Paths.Add("/api" + path.Key, path.Value);

        return Task.CompletedTask;
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("otp", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://tools.ietf.org/html/rfc6585#section-3",
            title = "Too many requests",
            status = StatusCodes.Status429TooManyRequests,
            errors = new[] { new { Code = "Common.RateLimited", Description = "Too many requests. Slow down and try again shortly." } }
        }, cancellationToken);
    };
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthenticationModule(builder.Configuration, builder.Environment);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCatalogPublicModule(builder.Configuration);
builder.Services.AddCatalogManagementModule(builder.Configuration);
builder.Services.AddShoppingCustomerModule(builder.Configuration);
builder.Services.AddOrderingCustomerModule(builder.Configuration);
builder.Services.AddOrderingManagementModule(builder.Configuration);
builder.Services.AddAdminManagementModule(builder.Configuration);
builder.Services.AddAdminProfileModule(builder.Configuration);
builder.Services.AddCustomerManagementModule(builder.Configuration);
builder.Services.AddCustomerProfileModule(builder.Configuration);
builder.Services.AddSellerManagementModule(builder.Configuration);
builder.Services.AddSellerProfileModule(builder.Configuration);
builder.Services.AddDriverManagementModule(builder.Configuration);
builder.Services.AddDriverProfileModule(builder.Configuration);
builder.Services.AddRolesManagementModule(builder.Configuration);
builder.Services.AddNotificationsModule(builder.Configuration);

builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "E-Commerce Store API";
        options.Theme = ScalarTheme.Mars;
        options.ShowSidebar = true;
        options.OpenApiRoutePattern = "/openapi/v1.json";
        options.Authentication = new ScalarAuthenticationOptions
        {
            PreferredSecuritySchemes = ["Bearer"]
        };
    });

    using var scope = app.Services.CreateScope();

    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseInfrastructure();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("StoreCorsPolicy");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("api").MapControllers();

app.MapHub<TrackingHub>(TrackingHub.HubPath);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true
});

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        logger.LogInformation(
            "Startup verification: database connection {Status}.",
            await dbContext.Database.CanConnectAsync() ? "established" : "unavailable");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Startup verification: database connectivity check failed.");
    }
}

app.Run();
