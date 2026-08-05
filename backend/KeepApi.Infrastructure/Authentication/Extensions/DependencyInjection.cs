using System.Text;
using KeepApi.Application.Interfaces;
using KeepApi.Infrastructure.Authentication.Jwt;
using KeepApi.Infrastructure.Authentication.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace KeepApi.Infrastructure.Authentication.Extensions
{
    public static class DependencyInjection
    {
        /// <summary>
        /// Infrastructure katmanına ait tüm auth/JWT kayıtları:
        /// JwtSettings binding, IJwtService/IAuthService, JWT Bearer authentication
        /// ve authorization. Host projesi (KeepApi) sadece bu metodu çağırır.
        /// </summary>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAuthService, AuthService>();

            var jwtSettings =
                configuration
                    .GetSection(JwtSettings.SectionName)
                    .Get<JwtSettings>()
                ?? throw new InvalidOperationException("Jwt configuration not found.");

            services
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
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = jwtSettings.ValidateIssuer,
                        ValidateAudience = jwtSettings.ValidateAudience,
                        ValidateLifetime = jwtSettings.ValidateLifetime,
                        ValidateIssuerSigningKey = jwtSettings.ValidateIssuerSigningKey,
                        ValidIssuer = jwtSettings.Issuer,
                        ValidAudience = jwtSettings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(jwtSettings.Key)),
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddAuthorization();

            return services;
        }
    }
}