using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("REFRESH_TOKENS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash)
                .HasColumnName("TOKEN_HASH")
                .HasColumnType("VARCHAR2(128)")
                .IsRequired();

            builder.HasIndex(x => x.TokenHash)
                .IsUnique();

            builder.Property(x => x.ExpiresAt)
                .HasColumnName("EXPIRES_AT");

            builder.Property(x => x.RevokedAt)
                .HasColumnName("REVOKED_AT");

            builder.Property(x => x.ReplacedByTokenHash)
                .HasColumnName("REPLACED_BY_TOKEN_HASH")
                .HasColumnType("VARCHAR2(128)");

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}