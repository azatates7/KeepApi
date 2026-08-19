using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class JobHistoryConfiguration
        : IEntityTypeConfiguration<JobHistory>
    {
        public void Configure(EntityTypeBuilder<JobHistory> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("ID")
                .HasColumnType("RAW(16)");

            builder.Property(x => x.JobDefinitionId)
                .HasColumnName("JOB_DEFINITION_ID")
                .HasColumnType("RAW(16)")
                .IsRequired();

            builder.Property(x => x.JobTypeId)
                .HasColumnName("JOB_TYPE_ID")
                .HasColumnType("NUMBER(10)")
                .IsRequired();

            builder.Property(x => x.TransactionId)
                .HasColumnName("TRANSACTION_ID")
                .HasColumnType("RAW(16)")
                .IsRequired();

            builder.Property(x => x.Username)
                .HasColumnName("USERNAME")
                .HasColumnType("NVARCHAR2(256)");

            builder.Property(x => x.StartedAt)
                .HasColumnName("STARTED_AT")
                .IsRequired();

            builder.Property(x => x.CompletedAt)
                .HasColumnName("COMPLETED_AT");

            builder.Property(x => x.ErrorMessage)
                .HasColumnName("ERROR_MESSAGE")
                .HasColumnType("NCLOB");

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

            // Her Job çalışmasının TransactionId'si benzersiz olmalı.
            builder.HasIndex(x => x.TransactionId)
                .IsUnique();

            builder.HasIndex(x => new
            {
                x.JobDefinitionId,
                x.StartedAt
            });

            builder.HasIndex(x => new
            {
                x.JobTypeId,
                x.StartedAt
            });

            builder.HasIndex(x => new
            {
                x.Username,
                x.StartedAt
            });

            // JobDefinition + JobTypeId birlikte eşleşmek zorunda.
            builder.HasOne(x => x.JobDefinition)
                .WithMany(x => x.JobHistories)
                .HasForeignKey(x => new
                {
                    x.JobDefinitionId,
                    x.JobTypeId
                })
                .HasPrincipalKey(x => new
                {
                    x.Id,
                    x.JobTypeId
                })
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}