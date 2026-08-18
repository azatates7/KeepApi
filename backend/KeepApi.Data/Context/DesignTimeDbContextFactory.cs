using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KeepApi.Data.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KeepDbContext>
    {
        public KeepDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = configuration.GetConnectionString("OracleConnection");

            var optionsBuilder = new DbContextOptionsBuilder<KeepDbContext>();

            optionsBuilder.UseOracle(connectionString)
                 .EnableDetailedErrors();

            return new KeepDbContext(optionsBuilder.Options);
        }
    }
}
