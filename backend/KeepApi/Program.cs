using KeepApi.Application.Interfaces;
using KeepApi.Common;
using KeepApi.Common.Security;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Data.Extensions;
using KeepApi.Data.Seed;
using KeepApi.Infrastructure.Authentication.Extensions;
using KeepApi.Infrastructure.Authentication.Services;
using KeepApi.Infrastructure.Configuration;
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

var builder = WebApplication.CreateBuilder(args);

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

    options.Lockout.MaxFailedAccessAttempts = 5;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<KeepDbContext>()
.AddDefaultTokenProviders();

// --- DB'den ayarları yükle (bootstrap aşaması, DI container henüz yok) ---
var bootstrapConnectionString = builder.Configuration.GetConnectionString("OracleConnection")
    ?? throw new InvalidOperationException("OracleConnection appsettings.json içinde tanımlı olmalı.");

var keyRingPath = builder.Configuration["DataProtection:KeyPath"] ?? @"C:\dp-keys";
var bootstrapProtectionProvider = DataProtectionProvider.Create(
    new DirectoryInfo(keyRingPath),
    opts => opts.SetApplicationName("KeepApi"));

IConfigurationBuilder configBuilder = builder.Configuration;
configBuilder.Add(new DbSettingsConfigurationSource(bootstrapConnectionString, "KeepApi", bootstrapProtectionProvider));

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

builder.Services.AddHttpClient<ILlmClient, GeminiLlmClient>();
builder.Services.AddScoped<DailySummaryService>();
builder.Services.AddScoped<AttachmentSummaryService>();

builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("DailySummaryJob");
    q.AddJob<DailySummaryJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("DailySummaryJob-trigger")
        .WithCronSchedule("0 0 8 * * ?", x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"))));
    // Linux container'da "Turkey Standard Time" bulunamazsa "Europe/Istanbul" kullanılmalı
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

app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<LoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();