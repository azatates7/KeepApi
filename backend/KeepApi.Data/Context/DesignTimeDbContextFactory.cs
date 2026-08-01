using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Data.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<KeepDbContext>
    {
        public KeepDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<KeepDbContext>();

            optionsBuilder.UseOracle(
                "User Id=SYSTEM;Password=oRaclePassWord43;Data Source=localhost:1521/xe")
                 .EnableDetailedErrors();

            return new KeepDbContext(optionsBuilder.Options);
        }
    }
}
