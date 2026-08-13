using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KeepApi.Data.Configurations
{
    public class NoteConfiguration : IEntityTypeConfiguration<Note>
    {
        public void Configure(EntityTypeBuilder<Note> builder)
        {
            builder.ToTable("NOTES");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("ID")
                .HasColumnType("VARCHAR2(32)");

            builder.Property(x => x.Title)
                .HasColumnName("TITLE")
                .HasColumnType("NVARCHAR2(300)");

            builder.Property(x => x.Content)
                .HasColumnName("CONTENT")
                .HasColumnType("NCLOB");

            builder.Property(x => x.Color)
                .HasColumnName("COLOR")
                .HasColumnType("VARCHAR2(30)")
                .HasDefaultValue("default");

            builder.Property(x => x.Pinned)
                .HasColumnName("PINNED");
                //.HasConversion<int>();//For Oracle

            builder.Property(x => x.Archived)
                .HasColumnName("ARCHIVED");
                //.HasConversion<int>();//For Oracle

            builder.Property(x => x.CreatedAt)
                .HasColumnName("CREATED_AT");

            builder.Property(x => x.UpdatedAt)
                .HasColumnName("UPDATED_AT");

            builder.Property(x => x.ReminderAt)
                .HasColumnName("REMINDER_AT");

            builder.Property(x => x.PinnedAt)
                .HasColumnName("PINNED_AT");

            builder.Property(x => x.ArchievedAt)
                .HasColumnName("ARCHIEVED_AT");

            builder.Property(x => x.Status)
                .HasColumnName("STATUS")
                .HasColumnType("NUMBER(10)");
                //.HasConversion<int>();

            builder.Property(x => x.IsDeleted)
                .HasColumnName("IS_DELETED");
                //.HasConversion<int>();//For Oracle

            builder.Property(x => x.Checklist)
                .HasColumnName("Checklist");
            //.HasConversion<int>();

            builder.Property(x => x.ImageAdded)
                .HasColumnName("ImageAdded");
            //.HasConversion<int>();

            builder.Property(x => x.IsDailySummary)
                .HasColumnName("IS_DAILY_SUMMARY");
                //.HasConversion<int>();//For Oracle
        }
    }
}
