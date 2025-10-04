using System.Data.Entity;
using System.Data.Entity.Infrastructure.Annotations;
using System.ComponentModel.DataAnnotations.Schema;
using OnlyOfficeServerFramework.Models;

namespace OnlyOfficeServerFramework.Data
{
    [DbConfigurationType(typeof(SqliteConfiguration))]
    public class AppDbContext : DbContext
    {
        // Static constructor to register SQLite provider
        static AppDbContext()
        {
            // Register SQLite EF6 factory
            System.Data.Common.DbProviderFactories.GetFactory("System.Data.SQLite.EF6");
        }

        public AppDbContext() : base("name=DefaultConnection")
        {
            // SQLite doesn't support CreateDatabaseIfNotExists
            // Use null initializer - database will be created on first access
            Database.SetInitializer<AppDbContext>(null);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<FileEntity> Files { get; set; }
        public DbSet<Installation> Installations { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<User>()
                .Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(e => e.Password)
                .IsRequired()
                .HasMaxLength(255);

            // Unique index on Username (EF6 uses IndexAnnotation)
            modelBuilder.Entity<User>()
                .Property(e => e.Username)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_Username") { IsUnique = true }));

            // Configure one-to-many relationship with Files
            modelBuilder.Entity<User>()
                .HasMany(e => e.Files)
                .WithRequired(f => f.User)
                .HasForeignKey(f => f.UserId)
                .WillCascadeOnDelete(true);

            // Configure FileEntity entity
            modelBuilder.Entity<FileEntity>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<FileEntity>()
                .Property(e => e.Filename)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<FileEntity>()
                .Property(e => e.OriginalName)
                .IsRequired()
                .HasMaxLength(255);

            modelBuilder.Entity<FileEntity>()
                .Property(e => e.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            // Configure Installation entity
            modelBuilder.Entity<Installation>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Installation>()
                .Property(e => e.BaseUrl)
                .IsRequired()
                .HasMaxLength(500);

            // Unique index on ApplicationId (EF6 uses IndexAnnotation)
            modelBuilder.Entity<Installation>()
                .Property(e => e.ApplicationId)
                .HasColumnAnnotation(
                    IndexAnnotation.AnnotationName,
                    new IndexAnnotation(new IndexAttribute("IX_ApplicationId") { IsUnique = true }));
        }
    }
}
