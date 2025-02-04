using DotNetEnv;
using ECommerceAPI;
using ECommerceAPI.Data;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Scalar.AspNetCore;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

Env.Load();
config.AddEnvironmentVariables();

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContextFactory<DataContext>(options =>
{
    options.UseSqlServer(config.GetConnectionString("DefaultConnection"));
}, ServiceLifetime.Scoped);

builder.Services.AddHttpClient();

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection = config.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"]!;
        options.ClientSecret = googleAuthNSection["ClientSecret"]!;
        options.Events.OnCreatingTicket = ctx =>
        {
            var name = ctx.Principal!.FindFirst(ClaimTypes.Name)?.Value;
            var dob = ctx.Principal.FindFirst(ClaimTypes.DateOfBirth)?.Value; // DOB may not be available
            var gender = ctx.Principal.FindFirst(ClaimTypes.Gender)?.Value; // Gender may not be available

            if (ctx.Principal!.Identity is ClaimsIdentity identity)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    identity.AddClaim(new Claim("Name", name));
                }
                if (!string.IsNullOrEmpty(dob))
                {
                    identity.AddClaim(new Claim("DOB", dob));
                }
                if (!string.IsNullOrEmpty(gender))
                {
                    identity.AddClaim(new Claim("Gender", gender));
                }
            }

            return Task.CompletedTask;
        };
    })
    .AddFacebook(options =>
    {
        IConfigurationSection FBAuthNSection = config.GetSection("Authentication:Facebook");
        options.AppId = FBAuthNSection["AppId"]!;
        options.AppSecret = FBAuthNSection["AppSecret"]!;
        options.Fields.Add("birthday"); // To request DOB (date of birth)
        options.Fields.Add("gender");   // To request Gender

        options.Events.OnCreatingTicket = ctx =>
        {
            var name = ctx.Principal!.FindFirst(ClaimTypes.Name)?.Value;
            var dob = ctx.Principal.FindFirst("birthday")?.Value; // Facebook-specific claim
            var gender = ctx.Principal.FindFirst("gender")?.Value; // Facebook-specific claim

            if (ctx.Principal!.Identity is ClaimsIdentity identity)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    identity.AddClaim(new Claim("Name", name));
                }
                if (!string.IsNullOrEmpty(dob))
                {
                    identity.AddClaim(new Claim("DOB", dob));
                }
                if (!string.IsNullOrEmpty(gender))
                {
                    identity.AddClaim(new Claim("Gender", gender));
                }
            }

            return Task.CompletedTask;
        };
    })
    .AddMicrosoftAccount(microsoftOptions =>
    {
        microsoftOptions.ClientId = config["Authentication:Microsoft:ClientId"]!;
        microsoftOptions.ClientSecret = config["Authentication:Microsoft:ClientSecret"]!;
        microsoftOptions.Events.OnCreatingTicket = ctx =>
        {
            var name = ctx.Principal!.FindFirst(ClaimTypes.Name)?.Value;
            var dob = ctx.Principal.FindFirst(ClaimTypes.DateOfBirth)?.Value; // DOB may not be available
            var gender = ctx.Principal.FindFirst(ClaimTypes.Gender)?.Value; // Gender may not be available

            if (ctx.Principal!.Identity is ClaimsIdentity identity)
            {
                if (!string.IsNullOrEmpty(name))
                {
                    identity.AddClaim(new Claim("Name", name));
                }
                if (!string.IsNullOrEmpty(dob))
                {
                    identity.AddClaim(new Claim("DOB", dob));
                }
                if (!string.IsNullOrEmpty(gender))
                {
                    identity.AddClaim(new Claim("Gender", gender));
                }
            }

            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddIdentityApiEndpoints<User>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<DataContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.CustomMapIdentityApi();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<User>>();

    await context.Database.MigrateAsync();

    var rolesResult = await SeedRolesAsync(roleManager);
    if (!rolesResult) return;

    var superUserResult = await SeedSuperUserAsync(userManager, config);
    if (!superUserResult) return;
}

app.Run();

static async Task<bool> SeedRolesAsync(RoleManager<IdentityRole> _roleManager)
{
    if (!await _roleManager.RoleExistsAsync("Admin"))
    {
        var result = await _roleManager.CreateAsync(new IdentityRole("Admin"));
        if (!result.Succeeded) return false;
    }

    if (!await _roleManager.RoleExistsAsync("Moderator"))
    {
        var result = await _roleManager.CreateAsync(new IdentityRole("Moderator"));
        if (!result.Succeeded) return false;
    }

    if (!await _roleManager.RoleExistsAsync("Transporter"))
    {
        var result = await _roleManager.CreateAsync(new IdentityRole("Transporter"));
        if (!result.Succeeded) return false;
    }

    return true;
}

static async Task<bool> SeedSuperUserAsync(UserManager<User> _userManager, IConfiguration _configuration)
{
    var superUserData = _configuration.GetSection("SuperUser");
    var superUser = await _userManager.FindByEmailAsync(superUserData["Email"]!);

    if (superUser is null)
    {
        superUser = new User
        {
            Name = "admin",
            UserName = superUserData["Email"],
            Email = superUserData["Email"],
            EmailConfirmed = true,
            PhoneNumber = superUserData["PhoneNumber"],
            PhoneNumberConfirmed = true,
            Gender = Gender.Male
        };

        var userResult = await _userManager.CreateAsync(superUser, superUserData["Password"]!);
        if (!userResult.Succeeded) return false;

        var roleResult = await _userManager.AddToRoleAsync(superUser, "Admin");
        if(!roleResult.Succeeded) return false;
    }
    return true;
}

