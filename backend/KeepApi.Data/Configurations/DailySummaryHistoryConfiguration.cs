using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class DailySummaryHistoryConfiguration : IEntityTypeConfiguration<DailySummaryHistory>
    {
        public void Configure(EntityTypeBuilder<DailySummaryHistory> builder)
        {
            builder.ToTable("DAILY_SUMMARY_HISTORIES");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Content)
                .HasColumnType("NCLOB")
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.UserId, x.GeneratedAt });
        }
    }
}