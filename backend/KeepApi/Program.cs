using KeepApi.Application.Interfaces;
using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using KeepApi.Data.Extensions;
using KeepApi.Infrastructure.Authentication.Extensions;
using KeepApi.Infrastructure.Authentication.Services;
using KeepApi.Middleware;
using KeepApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Serilog;
using StackExchange.Redis;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddScoped<NoteService>(); 
builder.Services.AddControllers();
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

builder.Services.AddSingleton<IConnectionMultiplexer>(sp => // Redis
{
    return ConnectionMultiplexer.Connect("localhost:6379");
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

// JWT Bearer authentication, authorization) KeepApi.Infrastructure katmanında kurulu.
// AddIdentity'den Sonra çağrılmalı; aksi halde Identity'nin varsayılan cookie şemasını geçersiz kılmaz.
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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
        var context = services.GetRequiredService<KeepDbContext>();
        var userManager =
            services.GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager =
            services.GetRequiredService<RoleManager<ApplicationRole>>();

        await DatabaseSeeder.SeedAsync(
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
app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<LoggingMiddleware>();
app.MapControllers();

app.Run();