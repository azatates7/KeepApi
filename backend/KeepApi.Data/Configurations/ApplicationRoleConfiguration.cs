using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class ApplicationRoleConfiguration
    : IEntityTypeConfiguration<ApplicationRole>
    {
        public void Configure(
            EntityTypeBuilder<ApplicationRole> builder)
        {
        }
    }
}
