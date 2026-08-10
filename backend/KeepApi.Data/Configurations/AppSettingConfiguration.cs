// KeepApi.Data/Configurations/AppSettingConfiguration.cs
using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
    {
        public void Configure(EntityTypeBuilder<AppSetting> builder)
        {
            builder.ToTable("APP_SETTINGS");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Key)
                .HasColumnName("SETTING_KEY")
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Value)
                .HasColumnName("SETTING_VALUE")
                .HasColumnType("NCLOB")
                .IsRequired();

            builder.Property(x => x.IsEncrypted)
                .HasColumnName("IS_ENCRYPTED");

            builder.Property(x => x.Description)
                .HasColumnName("DESCRIPTION")
                .HasMaxLength(500);

            builder.Property(x => x.TargetProject)
                .HasColumnName("TARGET_PROJECT")
                .HasMaxLength(100)
                .IsRequired();

            // Aynı proje içinde key tekrarını engelle; farklı projeler aynı key'i kullanabilir
            builder.HasIndex(x => new { x.Key, x.TargetProject }).IsUnique();
        }
    }
}