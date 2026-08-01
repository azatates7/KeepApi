using KeepApi.Data.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

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
        public DbSet<Note> Notes => Set<Note>();
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(typeof(KeepDbContext).Assembly);

            base.OnModelCreating(builder);

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
            });
        }
    }
}
