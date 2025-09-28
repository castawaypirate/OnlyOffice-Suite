using System.ComponentModel.DataAnnotations;

namespace OnlyOfficeServer.Models;

public class Installation
{
    public int Id { get; set; }

    [Required]
    public int ApplicationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Ip { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string FullUrl { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string DomainName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}