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
            //if (recordCount > 0) // Sadece tablo boşsa çalış // Eklenmesi gereken tekil kayıtlar varsa bir alt satıra alınabilir, altında olmayanları ekleme filtresi var
            //    return;

            var settings = new List<AppSetting>
            {/// UPDATE APP_SETTINGS SET SETTING_VALUE = 'openai' WHERE SETTING_KEY = 'Llm:Provider' AND TARGET_PROJECT = 'KeepApi';
                Plain("Llm:Provider", "gemini", "LLM provider", "KeepApi"),
                Plain("Llm:Model", "gemini-3.6-flash", "LLM model", "KeepApi"),
                Plain("Llm:BaseUrl", "https://generativelanguage.googleapis.com/v1beta", "LLM base url", "KeepApi"),
                Secret("Llm:ApiKey", "AQ.-***************", "LLM API key", "KeepApi", crypto),

                Plain("Llm:OpenAI:Model", "gpt-4o-mini", "OpenAI (ChatGPT) model", "KeepApi"),
                Plain("Llm:OpenAI:BaseUrl", "https://api.openai.com/v1", "OpenAI base url", "KeepApi"),
                Secret("Llm:OpenAI:ApiKey", "sk-proj-***************", "ChatGpt API Key", "KeepApi", crypto),

                Plain("Llm:Ollama:BaseUrl", "http://localhost:11434/v1", "Ollama base url", "KeepApi"),
                Plain("Llm:Ollama:Model", "gemma4:e4b", "Ollama text/vision model (natively multimodal)", "KeepApi"),

                Plain("Llm:Groq:BaseUrl", "https://api.groq.com/openai/v1", "Groq base url", "KeepApi"),
                Plain("Llm:Groq:Model", "openai/gpt-oss-20b", "Groq text model", "KeepApi"),
                Plain("Llm:Groq:VisionModel", "qwen/qwen3.6-27b", "Groq vision model (preview - Groq model sayfasından teyit et)", "KeepApi"),
                Secret("Llm:Groq:ApiKey", "gsk_***************", "Groq API Key", "KeepApi", crypto),

                Plain("Redis:ConnectionString", "localhost:6379", "Redis connection url", "KeepApi"),
                Plain("Cors:AllowedOrigin", "http://localhost:5173", "React frontend url", "KeepApi"),
                Plain("Jwt:Issuer", "KeepApi", "JWT issuer", "KeepApi"),
                Plain("Jwt:Audience", "KeepReact", "JWT audience", "KeepApi"),
                Plain("Jwt:ExpireMinutes", "60", "JWT token süresi (dk)", "KeepApi"),

                Plain("Jwt:RefreshTokenExpireDays", "14", "Refresh token geçerlilik süresi (gün)", "KeepApi"),

                Plain("Jwt:ValidateIssuer", "true", "JWT issuer doğrulama", "KeepApi"),
                Plain("Jwt:ValidateAudience", "true", "JWT audience doğrulama", "KeepApi"),
                Plain("Jwt:ValidateLifetime", "true", "JWT token geçerliliği", "KeepApi"),
                Plain("Jwt:ValidateIssuerSigningKey", "true", "JWT issuer imza anahtarı doğrulama", "KeepApi"),
                Secret("Jwt:Key", "***************", "JWT signing key", "KeepApi", crypto),

                Secret("ExternalProviders:Google:ClientId", "***************.apps.googleusercontent.com", "Google client ID", "KeepApi", crypto),
                Secret("ExternalProviders:Google:ClientSecret", "GOCSPX-***************", "Google client secret", "KeepApi", crypto),
                Secret("ExternalProviders:Microsoft:ClientId", "***************", "Microsoft client ID", "KeepApi", crypto),
                Secret("ExternalProviders:Microsoft:ClientSecret", "-mK8Q~h-***************~", "Microsoft client secret", "KeepApi", crypto),
                Secret("ExternalProviders:GitHub:ClientId", "***************", "GitHub client ID", "KeepApi", crypto),
                Secret("ExternalProviders:GitHub:ClientSecret", "***************", "GitHub client secret", "KeepApi", crypto),

                Plain("Smtp:Host", "smtp.gmail.com", "SMTP host", "KeepApi"),
                Plain("Smtp:Port", "587", "SMTP port", "KeepApi"),
                Plain("Smtp:User", "***************@gmail.com", "SMTP user", "KeepApi"),
                Secret("Smtp:Password", "***************", "Gmail app password", "KeepApi", crypto),
                Plain("Smtp:From", "***************@gmail.com", "SMTP from", "KeepApi"),
                Plain("Notifications:LoginFailureEmail", "***************@@gmail.com", "Başarısız giriş bildirim adresi", "KeepApi"),
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