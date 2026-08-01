using KeepApi.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KeepApi.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKeepData(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:Oracle"];
            services.AddDbContext<KeepDbContext>(options =>
            {
                options.UseOracle(connectionString)
                   .EnableSensitiveDataLogging()
                   .EnableDetailedErrors()
                   .LogTo(Console.WriteLine, LogLevel.Information);
            });

            return services;
        }
    }
}
