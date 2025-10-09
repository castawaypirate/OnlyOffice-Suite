using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<User> Users { get; set; }
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<Installation> Installations { get; set; }
    public DbSet<OnlyOfficeDocumentSession> OnlyOfficeDocumentSessions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
        });
        
        // Configure FileEntity entity
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UploadedAt).HasDefaultValueSql("datetime('now')");

            // Configure relationship
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Files)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Installation entity
        modelBuilder.Entity<Installation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ApplicationId).IsUnique();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("datetime('now')");
        });

        // Configure OnlyOfficeDocumentSession entity
        modelBuilder.Entity<OnlyOfficeDocumentSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OnlyOfficeToken).IsUnique();
            entity.HasIndex(e => new { e.FileId, e.OnlyOfficeToken, e.IsDeleted, e.ExpiresAt });
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("datetime('now')");
        });
    }
}