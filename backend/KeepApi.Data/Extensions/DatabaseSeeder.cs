using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using Microsoft.EntityFrameworkCore;

namespace KeepApi.Data.Extensions;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(KeepDbContext context)
    {
        try
        {
            var conn = context.Database.GetDbConnection();

            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "select count(*) from NOTES";

            var result = await cmd.ExecuteScalarAsync();
            var count = await context.Notes.CountAsync();

            if (count == 0)
            {
                await context.Database.MigrateAsync();

                var notes = new List<Note>
                {
                    new()
                    {
                        Title = "Welcome",
                        Content = "Test not oluşturuldu.",
                        Color = "default",
                        Pinned = true,
                        PinnedAt = DateTime.Now,
                        Archived = false,
                        ArchievedAt = null,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = null,
                        Status = 1,
                        IsDeleted = false
                    },
                    new()
                    {
                        Title = "Todo",
                        Content = "Oracle bağlantısını tamamla.",
                        Color = "yellow",
                        Pinned = false,
                        PinnedAt = null,
                        Archived = false,
                        ArchievedAt = null,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        Status = 1,
                        IsDeleted = false
                    }
                };

                await context.Notes.AddRangeAsync(notes);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw;
        }
    }
}