using KeepApi.Common.Security;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace KeepApi.Data.Seed
{
    public static class AppSettingsSeeder
    {
        public static async Task SeedAsync(KeepDbContext context, IAppSettingsCrypto crypto)
        {
            var recordCount = await context.AppSettings.CountAsync();
            if (recordCount > 0) // Sadece tablo boşsa çalış // Eklenmesi gereken tekil kayıtlar varsa bir alt satıra alınabilir, altında olmayanları ekleme filtresi var
                return;

            var settings = new List<AppSetting>
            {/// UPDATE APP_SETTINGS SET SETTING_VALUE = 'openai' WHERE SETTING_KEY = 'Llm:Provider' AND TARGET_PROJECT = 'KeepApi';
                Plain("Llm:Provider", "gemini", "LLM provider", "KeepApi"),
                Plain("Llm:Model", "gemini-3.6-flash", "LLM model", "KeepApi"),
                Plain("Llm:BaseUrl", "https://generativelanguage.googleapis.com/v1beta", "LLM base url", "KeepApi"),
                Secret("Llm:ApiKey", "AQ.Ab8RN6JjnK_QtxuedMdAr-UBh7WT8nlNiRh2Yx11O1PpU7mXbA", "LLM API key", "KeepApi", crypto),

                Plain("Llm:OpenAI:Model", "gpt-4o-mini", "OpenAI (ChatGPT) model", "KeepApi"),
                Plain("Llm:OpenAI:BaseUrl", "https://api.openai.com/v1", "OpenAI base url", "KeepApi"),
                Secret("Llm:OpenAI:ApiKey", "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", "ChatGpt API Key", "KeepApi", crypto),

                Plain("Llm:Ollama:BaseUrl", "http://localhost:11434/v1", "Ollama base url", "KeepApi"),
                Plain("Llm:Ollama:Model", "gemma4:e4b", "Ollama text/vision model (natively multimodal)", "KeepApi"),

                Plain("Redis:ConnectionString", "localhost:6379", "Redis connection url", "KeepApi"),
                Plain("Cors:AllowedOrigin", "http://localhost:5173", "React frontend url", "KeepApi"),
                Plain("Jwt:Issuer", "KeepApi", "JWT issuer", "KeepApi"),
                Plain("Jwt:Audience", "KeepReact", "JWT audience", "KeepApi"),
                Plain("Jwt:ExpireMinutes", "60", "JWT token süresi (dk)", "KeepApi"),
                Plain("Jwt:ValidateIssuer", "true", "JWT issuer doğrulama", "KeepApi"),
                Plain("Jwt:ValidateAudience", "true", "JWT audience doğrulama", "KeepApi"),
                Plain("Jwt:ValidateLifetime", "true", "JWT token geçerliliği", "KeepApi"),
                Plain("Jwt:ValidateIssuerSigningKey", "true", "JWT issuer imza anahtarı doğrulama", "KeepApi"),
                Secret("Jwt:Key", "THIS_IS_MY_SUPER_SECRET_KEY_MORE_THAN_32_CHARACTERS", "JWT signing key", "KeepApi", crypto),

                Secret("ExternalProviders:Google:ClientId", "646278578348-7e4a8tqtpavac5pbk72ctf5bkvdat7gr.apps.googleusercontent.com", "Google client ID", "KeepApi", crypto),
                Secret("ExternalProviders:Google:ClientSecret", "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", "Google client secret", "KeepApi", crypto),
                Secret("ExternalProviders:Microsoft:ClientId", "2f8ac164-ed89-4927-afa0-67dc1a67e58c", "Microsoft client ID", "KeepApi", crypto),
                Secret("ExternalProviders:Microsoft:ClientSecret", "-mK8Q~h-9cGEck8vlw540CZDnmH72dJlDnBH2am~", "Microsoft client secret", "KeepApi", crypto),
                Secret("ExternalProviders:GitHub:ClientId", "Ov23liY6P56DZDTTLzaz", "GitHub client ID", "KeepApi", crypto),
                Secret("ExternalProviders:GitHub:ClientSecret", "61fc5b3c04451d4b966033174d337d243c6b77c9", "GitHub client secret", "KeepApi", crypto),

                Plain("Smtp:Host", "smtp.gmail.com", "SMTP host", "KeepApi"),
                Plain("Smtp:Port", "587", "SMTP port", "KeepApi"),
                Plain("Smtp:User", "azatates4977@gmail.com", "SMTP user", "KeepApi"),
                Secret("Smtp:Password", "palf nwuh qctu mnyz", "Gmail app password", "KeepApi", crypto),
                Plain("Smtp:From", "azatates4977@gmail.com", "SMTP from", "KeepApi"),
                Plain("Smtp:EnableSsl", "true", "SMTP enable SSL", "KeepApi")
            };

            var existingKeys = (await context.AppSettings
                .Select(s => new { s.Key, s.TargetProject })
                .ToListAsync())
                .Select(s => (s.Key, s.TargetProject))
                .ToHashSet();

            var missingKeys = settings
                .Where(s => !existingKeys.Contains((s.Key, s.TargetProject)))
                .ToList();

            if (missingKeys.Count == 0)
                return;

            context.AppSettings.AddRange(missingKeys);
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