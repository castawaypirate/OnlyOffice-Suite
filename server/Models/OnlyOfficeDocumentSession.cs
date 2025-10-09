using System.ComponentModel.DataAnnotations;

namespace OnlyOfficeServer.Models;

public class OnlyOfficeDocumentSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid FileId { get; set; }

    [Required]
    [MaxLength(100)]
    public string OnlyOfficeToken { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? DocumentKey { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public string? Config { get; set; }
}
