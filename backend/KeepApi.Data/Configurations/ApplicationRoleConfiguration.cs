using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace KeepApi.Data.Configurations
{
    public class ApplicationRoleConfiguration
    : IEntityTypeConfiguration<ApplicationRole>
    {
        public void Configure(
            EntityTypeBuilder<ApplicationRole> builder)
        {
            builder.ToTable("APP_ROLES");
        }
    }
}
