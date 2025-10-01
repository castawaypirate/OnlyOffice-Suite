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
                Id = Guid.NewGuid(),
                Username = "admin",
                Password = "admin123",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "user1",
                Password = "password",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        context.SaveChanges();

        // Check if installations already exist
        if (!context.Installations.Any())
        {
            // Create installation record
            var installation = new Installation
            {
                ApplicationId = 1,
                Ip = "localhost",
                FullUrl = "http://localhost:5142",
                DomainName = "localhost",
                Description = "Local Development Installation",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Installations.Add(installation);
            context.SaveChanges();
        }
    }
}