using KeepApi.Data.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KeepApi.Data.Context
{
    public class KeepDbContext : 
        IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        Guid>
    {
        public KeepDbContext(DbContextOptions<KeepDbContext> options)
            : base(options)
        {
        }

        public DbSet<Note> Notes { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<DailySummaryHistory> DailySummaryHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(KeepDbContext).Assembly);

            base.OnModelCreating(builder);
            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(bool) && property.GetValueConverter() is null)
                    {
                        property.SetColumnType("NUMBER(1)");   // converter satırını sil
                    }
                    else if (property.ClrType == typeof(int) && property.GetValueConverter() is null)
                    {
                        property.SetValueConverter(
                            new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<int, long>(
                                v => v, v => (int)v));
                    }
                }
            }

            builder.Entity<IdentityUserClaim<Guid>>()
                .ToTable("APP_USER_CLAIMS");

            builder.Entity<IdentityUserLogin<Guid>>()
                .ToTable("APP_USER_LOGINS");

            builder.Entity<IdentityUserRole<Guid>>()
                .ToTable("APP_USER_ROLES");

            builder.Entity<IdentityRoleClaim<Guid>>()
                .ToTable("APP_ROLE_CLAIMS");

            builder.Entity<IdentityUserToken<Guid>>()
                .ToTable("APP_USER_TOKENS");

            builder.Entity<Note>(entity =>
            {
                entity.HasOne(x => x.User)
                      .WithMany(x => x.Notes)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.CreatedBy)
                      .WithMany()
                      .HasForeignKey(x => x.CreatedById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.UpdatedBy)
                      .WithMany()
                      .HasForeignKey(x => x.UpdatedById)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.DeletedBy)
                      .WithMany()
                      .HasForeignKey(x => x.DeletedById)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
