using KeepApi.Application.Interfaces;
using KeepApi.Common;
using KeepApi.Common.Security;
using KeepApi.Data.Configurations;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Data.Extensions;
using KeepApi.Data.Seed;
using KeepApi.Infrastructure.Authentication.Extensions;
using KeepApi.Infrastructure.Authentication.Services;
using KeepApi.Infrastructure.Configurations;
using KeepApi.Infrastructure.Llm;
using KeepApi.Jobs;
using KeepApi.Middleware;
using KeepApi.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Quartz;
using Serilog;
using Serilog.Events;
using StackExchange.Redis;
using System.Reflection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Connection string çözümü (env var / Windows Credential Manager) KeepApi.Data'da yaşıyor —
// API projesi ConnectionString'in NEREDEN geldiğini bilmiyor, sadece sonucu kullanıyor.
// Configuration'a geri yazılıyor ki hem builder.Services.AddKeepData hem de aşağıdaki
// DbSettingsConfigurationSource aynı, doğru çözülmüş değeri görsün.
var oracleConnectionString = OracleConnectionStringResolver.Resolve(builder.Configuration);
builder.Configuration["ConnectionStrings:OracleConnection"] = oracleConnectionString;

//Serilog.Debugging.SelfLog.Enable(msg => Console.Error.WriteLine(msg)); // Debug Serilog errors
//Log.Logger = new LoggerConfiguration()
//    .ReadFrom.Configuration(builder.Configuration)
//    .CreateLogger();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(evt =>
            evt.Level == LogEventLevel.Error || evt.Level == LogEventLevel.Fatal)
        .WriteTo.File(
            path: builder.Configuration["ErrorLogFileName"] ?? throw new Exception("ErrorLogFileName Not Found"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 90,
            shared: true))
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddScoped<NoteService>();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters
            .Add(new TrimStringJsonConverter()); // Trim global çözüm
    });
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Keep Todo API",
        Version = "v1",
        Description = "Oracle + ASP.NET Core Identity tabanlı not/todo API'si. Uçlar Bearer JWT ile korunur."
    });

    // NotesController'daki /// <summary> yorumlarının Swagger UI'da görünmesi için.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        //Description = "JWT Bearer token. Example: Bearer {token}",
        //Name = "Authorization",
        //In = ParameterLocation.Header,
        //Type = SecuritySchemeType.Http,
        //Scheme = "Bearer",
        //BearerFormat = "JWT"

        Description = "JWT Bearer token. \"Bearer \" ön ekiyle birlikte girin. Örnek: Bearer eyJhbGciOiJIUzI1NiIs...",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services.AddCors(options => // React policy allow
{
    var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// IP bazlı rate limiting: login zaten AuthService içinde DB'ye işlenen (AccessFailedCount)
// iki kademeli hesap kilitlemesine sahip; ancak register/forgot-password/verify-email gibi
// "doğru cevabı tahmin etme" mantığı olmayan endpoint'lerde hesap bazlı bir sayaç anlamsız —
// buralarda kaynak IP başına sabit pencereli bir limit uygulanır.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,                       // pencere başına izin verilen istek
                Window = TimeSpan.FromMinutes(15),      // pencere süresi
                QueueLimit = 0,                         // limit dolunca kuyruğa alma, doğrudan 429 dön
                AutoReplenishment = true
            }));
});

builder.Services.AddKeepData(builder.Configuration);

builder.Services
.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.User.RequireUniqueEmail = true;

    options.Password.RequiredLength = 8;

    options.Password.RequireDigit = true;

    options.Password.RequireUppercase = true;

    options.Password.RequireLowercase = true;

    options.Password.RequireNonAlphanumeric = false;

    // NOT: MaxFailedAccessAttempts/DefaultLockoutTimeSpan artık kullanılmıyor —
    // AuthService.LoginAsync kendi iki kademeli kilit mantığını (3 denemede 5 dk,
    // 10 denemede kalıcı) IncrementAccessFailedCountAsync/SetLockoutEndDateAsync ile
    // manuel yönetiyor. AllowedForNewUsers=true olduğu sürece bu ayarların değeri
    // IsLockedOutAsync tarafından kullanılmıyor, sadece Identity'nin varsayılan
    // AccessFailedAsync çağrılırsa (şu an çağrılmıyor) devreye girer.
    options.Lockout.MaxFailedAccessAttempts = 5;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<KeepDbContext>()
.AddDefaultTokenProviders();

// --- DB'den ayarları yükle (bootstrap aşaması, DI container henüz yok) ---
// oracleConnectionString yukarıda (builder oluşturulur oluşturulmaz) zaten çözüldü.

var keyRingPath = builder.Configuration["DataProtection:KeyPath"] ?? @"C:\dp-keys";
var bootstrapProtectionProvider = DataProtectionProvider.Create(
    new DirectoryInfo(keyRingPath),
    opts => opts.SetApplicationName("KeepApi"));

IConfigurationBuilder configBuilder = builder.Configuration;
configBuilder.Add(new DbSettingsConfigurationSource(oracleConnectionString, "KeepApi", bootstrapProtectionProvider));

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyRingPath))
    .SetApplicationName("KeepApi"); // bootstrap ile AYNI path + app name olmalı, aksi halde Unprotect patlar

builder.Services.AddScoped<IAppSettingsCrypto, AppSettingsCrypto>();

// JWT Bearer authentication, authorization) KeepApi.Infrastructure katmanında kurulu.
// AddIdentity'den Sonra çağrılmalı; aksi halde Identity'nin varsayılan cookie şemasını geçersiz kılmaz.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => // Redis
{
    var connectionString = builder.Configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException("Redis:ConnectionString tanımlı değil.");
    return ConnectionMultiplexer.Connect(connectionString.Replace(@"http://", string.Empty)); // Burada hata alınırsa Redis aktifleştirilmeli
});

//builder.Services.AddHttpClient<ILlmClient, GeminiLlmClient>();
// Llm:Provider ayarına göre "gemini", "openai", "ollama" veya "groq" istemcisi kaydedilir.
// DailySummaryService yalnızca ILlmClient'a bağımlı olduğu için provider
// değişimi bu tek yeri etkiler.
var llmProvider = builder.Configuration["Llm:Provider"] ?? "gemini";

if (string.Equals(llmProvider, "openai", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<ILlmClient, OpenAiLlmClient>();
}
else if (string.Equals(llmProvider, "ollama", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<ILlmClient, OllamaLlmClient>(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(3); // yerel model CPU'da yavaş olabilir
    });
}
else if (string.Equals(llmProvider, "groq", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<ILlmClient, GroqLlmClient>();
}
else
{
    builder.Services.AddHttpClient<ILlmClient, GeminiLlmClient>();
}

builder.Services.AddScoped<DailySummaryService>();
builder.Services.AddScoped<AttachmentSummaryService>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("DailySummaryJob");
    q.AddJob<DailySummaryJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("DailySummaryJob-trigger")
        .WithCronSchedule("0 57 16 * * ?", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"))));
    // Linux container'da "Turkey Standard Time" bulunamazsa "Europe/Istanbul" kullanılmalı

    var reminderJobKey = new JobKey("ReminderNotificationJob");
    q.AddJob<ReminderNotificationJob>(opts => opts.WithIdentity(reminderJobKey));
    q.AddTrigger(opts => opts
        .ForJob(reminderJobKey)
        .WithIdentity("ReminderNotificationJob-trigger")
        .WithSimpleSchedule(x => x
            .WithIntervalInMinutes(5)
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(opts => opts.WaitForJobsToComplete = true);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Keep Todo API v1");
    });
}

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context =
            services.GetRequiredService<KeepDbContext>();

        await context.Database.MigrateAsync(); // Seeder'lardan önce şema oluşturulmalı

        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager =
            services.GetRequiredService<RoleManager<ApplicationRole>>();

        var crypto = services.GetRequiredService<IAppSettingsCrypto>();
        await AppSettingsSeeder.SeedAsync(context, crypto);

        await UserSeeder.SeedAsync(
            context,
            userManager,
            roleManager);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı seed işlemi sırasında bir hata oluştu.");
    }
}

app.UseCors("AllowFrontend");

app.UseRateLimiter();

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();