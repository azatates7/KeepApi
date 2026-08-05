using System.Text;
using KeepApi.Application.Interfaces;
using KeepApi.Infrastructure.Authentication.Jwt;
using KeepApi.Infrastructure.Authentication.PasswordReset;
using KeepApi.Infrastructure.Authentication.Services;
using KeepApi.Infrastructure.Email;
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
        /// JwtSettings binding, IJwtService/IAuthService, JWT Bearer authentication,
        /// authorization, şifre sıfırlama kodu deposu ve e-posta gönderimi.
        /// Host projesi (KeepApi) sadece bu metodu çağırır.
        /// </summary>
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<JwtSettings>(
                configuration.GetSection(JwtSettings.SectionName));

            services.Configure<SmtpSettings>(
                configuration.GetSection(SmtpSettings.SectionName));

            services.AddScoped<IJwtService, JwtService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IEmailService, SmtpEmailService>();

            // Şifre sıfırlama kodları kısa ömürlü (10 dk) olduğu için tek instance'lık
            // bellek içi depo yeterli; birden fazla API instance'ı (load balancer arkasında)
            // çalıştırırsanız bunun yerine Redis tabanlı bir implementasyon gerekir.
            services.AddSingleton<IPasswordResetCodeStore, InMemoryPasswordResetCodeStore>();

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