// KeepApi.Data/Seed/AppSettingsSeeder.cs
using KeepApi.Common.Security;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;

namespace KeepApi.Data.Seed
{
    public static class AppSettingsSeeder
    {
        public static async Task SeedAsync(KeepDbContext context, IAppSettingsCrypto crypto)
        {
            if (context.AppSettings.Any())
                return; // sadece tablo boşsa çalış

            var settings = new List<AppSetting>
            {
                Plain("Cors:AllowedOrigin", "http://localhost:5173", "React frontend url", "KeepApi"),

                Plain("Jwt:Issuer", "KeepApi", "JWT issuer", "KeepApi"),
                Plain("Jwt:Audience", "KeepReact", "JWT audience", "KeepApi"),
                Plain("Jwt:ExpireMinutes", "60", "JWT token süresi (dk)", "KeepApi"),
                Plain("Jwt:ValidateIssuer", "true", "", "KeepApi"),
                Plain("Jwt:ValidateAudience", "true", "", "KeepApi"),
                Plain("Jwt:ValidateLifetime", "true", "", "KeepApi"),
                Plain("Jwt:ValidateIssuerSigningKey", "true", "", "KeepApi"),
                Secret("Jwt:Key", "THIS_IS_MY_SUPER_SECRET_KEY_MORE_THAN_32_CHARACTERS", "JWT signing key", "KeepApi", crypto),

                Secret("ExternalProviders:Google:ClientId", "646278578348-7e4a8tqtpavac5pbk72ctf5bkvdat7gr.apps.googleusercontent.com", "", "KeepApi", crypto),
                Secret("ExternalProviders:Google:ClientSecret", "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", "", "KeepApi", crypto),
                Secret("ExternalProviders:Microsoft:ClientId", "2f8ac164-ed89-4927-afa0-67dc1a67e58c", "", "KeepApi", crypto),
                Secret("ExternalProviders:Microsoft:ClientSecret", "-mK8Q~h-9cGEck8vlw540CZDnmH72dJlDnBH2am~", "", "KeepApi", crypto),
                Secret("ExternalProviders:GitHub:ClientId", "Ov23liY6P56DZDTTLzaz", "", "KeepApi", crypto),
                Secret("ExternalProviders:GitHub:ClientSecret", "61fc5b3c04451d4b966033174d337d243c6b77c9", "", "KeepApi", crypto),

                Plain("Smtp:Host", "smtp.gmail.com", "", "KeepApi"),
                Plain("Smtp:Port", "587", "", "KeepApi"),
                Plain("Smtp:User", "azatates4977@gmail.com", "", "KeepApi"),
                Secret("Smtp:Password", "palf nwuh qctu mnyz", "Gmail app password", "KeepApi", crypto),
                Plain("Smtp:From", "azatates4977@gmail.com", "", "KeepApi"),
                Plain("Smtp:EnableSsl", "true", "", "KeepApi"),
            };

            context.AppSettings.AddRange(settings);
            await context.SaveChangesAsync();
        }

        private static AppSetting Plain(string key, string value, string desc, string project) => new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = value,
            IsEncrypted = false,
            Description = desc,
            TargetProject = project
        };

        private static AppSetting Secret(string key, string value, string desc, string project, IAppSettingsCrypto crypto) => new()
        {
            Id = Guid.NewGuid(),
            Key = key,
            Value = crypto.Encrypt(value),
            IsEncrypted = true,
            Description = desc,
            TargetProject = project
        };
    }
}