using KeepApi.Data.Context;
using KeepApi.Data.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KeepApi.Data.Seed
{
    public static class UserSeeder
    {
        public static async Task SeedAsync(
            KeepDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager)
        {
            try
            {
                var conn = context.Database.GetDbConnection();

                await conn.OpenAsync();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "select count(*) from NOTES";

                var result = await cmd.ExecuteScalarAsync();
                var countOfRoles = await context.Roles.CountAsync();
                if (countOfRoles == 0)
                {
                    string[] roles =
                    {
                    "Admin",
                    "User"
                };

                    foreach (var role in roles)
                    {
                        if (!await roleManager.RoleExistsAsync(role))
                        {
                            await roleManager.CreateAsync(new ApplicationRole
                            {
                                Name = role
                            });
                        }
                    }
                }

                var admin = await userManager.FindByNameAsync("admin");
                var user = await userManager.FindByNameAsync("testUser");

                if (admin == null)
                {
                    admin = new ApplicationUser
                    {
                        UserName = "admin",
                        Email = "admin@test.com",
                        EmailConfirmed = true,
                        FirstName = "System",
                        LastName = "Administrator",
                        CreatedAt = DateTime.Now,
                        IsDeleted = false,
                        Status = 1
                    };

                    var createResult = await userManager.CreateAsync(admin, "Admin123!");

                    if (!createResult.Succeeded)
                    {
                        throw new Exception(string.Join(",", createResult.Errors.Select(x => x.Description)));
                    }
                }

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = "testUser",
                        Email = "user@test.com",
                        EmailConfirmed = true,
                        FirstName = "Test",
                        LastName = "User",
                        CreatedAt = DateTime.Now,
                        IsDeleted = false,
                        Status = 1
                    };

                    var createResult = await userManager.CreateAsync(user, "User123!");

                    if (!createResult.Succeeded)
                    {
                        throw new Exception(string.Join(",", createResult.Errors.Select(x => x.Description)));
                    }
                }

                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                }

                if (!await userManager.IsInRoleAsync(user, "User"))
                {
                    await userManager.AddToRoleAsync(user, "User");
                }

                var countOfNote = await context.Notes.CountAsync();

                if (countOfNote == 0)
                {
                    await context.Database.MigrateAsync();

                    var notes = new List<Note>
                {
                    new()
                    {
                        Title = "Welcome2",
                        Content = "Test not oluşturuldu.",
                        Color = "default",
                        Pinned = true,
                        PinnedAt = DateTime.Now,
                        Archived = false,
                        CreatedAt = DateTime.Now,
                        Status = 1,
                        IsDeleted = false,
                        User = admin,
                        UserId = admin.Id
                    },
                    new()
                    {
                        Title = "Todo2",
                        Content = "Oracle bağlantısını tamamla.",
                        Color = "yellow",
                        CreatedAt = DateTime.Now,
                        Status = 1,
                        IsDeleted = false,
                        User = admin,
                        UserId = admin.Id
                    },
                    new()
                    {
                        Title = "TodoUser",
                        Content = "Oracle bağlantısını gerçekleştir.",
                        Color = "green",
                        CreatedAt = DateTime.Now,
                        Status = 1,
                        IsDeleted = false,
                        User = user,
                        UserId = user.Id
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
}