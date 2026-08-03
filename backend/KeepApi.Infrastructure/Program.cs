using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Data.Extensions;
using KeepApi.Infrastructure.Authentication.Extensions;
using KeepApi.Infrastructure.Authentication.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Configuration;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKeepData(builder.Configuration);

builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings =
    builder.Configuration
        .GetSection(JwtSettings.SectionName)
        .Get<JwtSettings>()
    ?? throw new InvalidOperationException(
        "Jwt configuration not found.");

builder.Services
.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;

    options.Password.RequireDigit = true;

    options.Password.RequireLowercase = true;

    options.Password.RequireUppercase = true;

    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;

    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(15);

    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<KeepDbContext>()
.AddDefaultTokenProviders();

builder.Services
.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultChallengeScheme =
        JwtBearerDefaults.AuthenticationScheme;

    options.DefaultScheme =
        JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
});

var tokenValidationParameters =
    new TokenValidationParameters
    {
        ValidateIssuer =
            jwtSettings.ValidateIssuer,

        ValidateAudience =
            jwtSettings.ValidateAudience,

        ValidateLifetime =
            jwtSettings.ValidateLifetime,

        ValidateIssuerSigningKey =
            jwtSettings.ValidateIssuerSigningKey,

        ValidIssuer =
            jwtSettings.Issuer,

        ValidAudience =
            jwtSettings.Audience,

        IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.Key)),

        ClockSkew = TimeSpan.Zero
    };

var app = builder.Build();

app.Run();