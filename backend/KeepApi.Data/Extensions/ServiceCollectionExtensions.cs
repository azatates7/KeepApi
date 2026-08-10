using KeepApi.Data.Context;
using KeepApi.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KeepApi.Data.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddKeepData(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration["ConnectionStrings:OracleConnection"];
            //services.AddDbContext<KeepDbContext>(options =>
            //{
            //    options.UseOracle(connectionString)
            //       .EnableDetailedErrors();
            //});

            services.AddScoped<AuditInterceptor>();

            services.AddDbContext<KeepDbContext>((sp, options) =>
            {
                options.UseOracle(connectionString);

                options.AddInterceptors(
                    sp.GetRequiredService<AuditInterceptor>());
            });

            return services;
        }
    }
}
