using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Services;

public static class DatabaseSeederService
{
    public static void SeedDatabase(AppDbContext context)
    {
        // Check if users already exist
        if (context.Users.Any())
        {
            return; // Database already seeded
        }

        // Create test users
        var users = new List<User>
        {
            new User
            {
                Username = "admin",
                Password = "admin123",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "user1",
                Password = "password",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();
    }
}