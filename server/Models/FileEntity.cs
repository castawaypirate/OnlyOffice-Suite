using System.ComponentModel.DataAnnotations;

namespace OnlyOfficeServer.Models;

public class FileEntity
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    [Required]
    [MaxLength(255)]
    public string Filename { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(255)]
    public string OriginalName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public User User { get; set; } = null!;
}