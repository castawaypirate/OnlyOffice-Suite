using System;
using System.Linq;
using System.IO;
using System.Data.SQLite;
using OnlyOfficeServerFramework.Data;
using OnlyOfficeServerFramework.Models;

namespace OnlyOfficeServerFramework.Services
{
    public class DatabaseSeederService
    {
        public static void SeedDatabase(AppDbContext context)
        {
            // Get the database file path from connection string
            var connectionString = context.Database.Connection.ConnectionString;
            var builder = new SQLiteConnectionStringBuilder(connectionString);
            var dbPath = builder.DataSource;

            // Resolve |DataDirectory| token
            if (dbPath.Contains("|DataDirectory|"))
            {
                var dataDirectory = AppDomain.CurrentDomain.GetData("DataDirectory") as string;
                if (string.IsNullOrEmpty(dataDirectory))
                {
                    dataDirectory = AppDomain.CurrentDomain.BaseDirectory + "App_Data";
                }
                dbPath = dbPath.Replace("|DataDirectory|", dataDirectory);
            }

            // Ensure App_Data directory exists
            var directory = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create the database file if it doesn't exist
            if (!File.Exists(dbPath))
            {
                SQLiteConnection.CreateFile(dbPath);
                Console.WriteLine($"[SEEDER] Created database file at: {dbPath}");
            }

            // Create tables using EF Code First
            // This will execute the SQL to create tables based on our models
            context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS Users (
                    Id TEXT PRIMARY KEY,
                    Username TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL
                )");

            context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS Files (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    Filename TEXT NOT NULL,
                    OriginalName TEXT NOT NULL,
                    FilePath TEXT NOT NULL,
                    UploadedAt TEXT NOT NULL,
                    LastModifiedAt TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
                )");

            context.Database.ExecuteSqlCommand(@"
                CREATE TABLE IF NOT EXISTS Installations (
                    Id TEXT PRIMARY KEY,
                    ApplicationId INTEGER NOT NULL UNIQUE,
                    BaseUrl TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                )");

            // Seed Users if empty
            if (!context.Users.Any())
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "admin",
                    Password = "admin123", // Plain text for POC (matches .NET 9 server)
                    CreatedAt = DateTime.UtcNow
                };

                var testUser = new User
                {
                    Id = Guid.NewGuid(),
                    Username = "user1",
                    Password = "password", // Plain text for POC
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(adminUser);
                context.Users.Add(testUser);
                context.SaveChanges();

                Console.WriteLine("[SEEDER] Created test users: admin/admin123, user1/password");
            }
            else
            {
                Console.WriteLine("[SEEDER] Users already exist, skipping user seeding");
            }

            // Seed Installations if empty
            if (!context.Installations.Any())
            {
                var installation = new Installation
                {
                    Id = Guid.NewGuid(),
                    ApplicationId = 1,
                    BaseUrl = "http://localhost:5142",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.Installations.Add(installation);
                context.SaveChanges();

                Console.WriteLine("[SEEDER] Created default installation with ApplicationId=1");
            }
            else
            {
                Console.WriteLine("[SEEDER] Installations already exist, skipping installation seeding");
            }
        }
    }
}
