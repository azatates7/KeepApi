using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class JobDefinitionConfiguration
        : IEntityTypeConfiguration<JobDefinition>
    {
        public void Configure(EntityTypeBuilder<JobDefinition> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("ID")
                .HasColumnType("RAW(16)");

            builder.Property(x => x.JobTypeId)
                .HasColumnName("JOB_TYPE_ID")
                .HasColumnType("NUMBER(10)")
                .IsRequired();

            builder.Property(x => x.JobName)
                .HasColumnName("JOB_NAME")
                .HasColumnType("NVARCHAR2(200)")
                .IsRequired();

            builder.Property(x => x.Description)
                .HasColumnName("DESCRIPTION")
                .HasColumnType("NVARCHAR2(500)");

            builder.Property(x => x.CronExpression)
                .HasColumnName("CRON_EXPRESSION")
                .HasColumnType("VARCHAR2(100)")
                .IsRequired();

            builder.Property(x => x.IsActive)
                .HasColumnName("IS_ACTIVE")
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnName("CREATED_AT")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("UPDATED_AT");

            builder.Property(x => x.Status)
                .HasColumnName("STATUS")
                .HasColumnType("NUMBER(10)")
                .IsRequired();

            builder.Property(x => x.IsDeleted)
                .HasColumnName("IS_DELETED")
                .IsRequired();

            builder.HasIndex(x => x.JobTypeId);

            builder.HasIndex(x => new
            {
                x.JobTypeId,
                x.IsActive
            });
        }
    }
}