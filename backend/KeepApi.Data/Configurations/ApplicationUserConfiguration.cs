using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class ApplicationUserConfiguration
    : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(
            EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.Property(x => x.FirstName)
                .HasMaxLength(100);

            builder.Property(x => x.LastName)
                .HasMaxLength(100);

            builder.Property(x => x.IsDeleted)
                .HasDefaultValue(false);

            builder.Property(x => x.PreferredLanguage)
                .HasMaxLength(5)
                .HasDefaultValue("tr");
        }
    }
}
