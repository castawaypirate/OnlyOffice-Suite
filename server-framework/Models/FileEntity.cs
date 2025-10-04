using System;
using System.ComponentModel.DataAnnotations;

namespace OnlyOfficeServerFramework.Models
{
    public class FileEntity
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string Filename { get; set; }

        [Required]
        [MaxLength(255)]
        public string OriginalName { get; set; }

        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; }

        public DateTime UploadedAt { get; set; }

        public DateTime LastModifiedAt { get; set; }

        // Navigation property
        public virtual User User { get; set; }

        public FileEntity()
        {
            Id = Guid.NewGuid();
            UploadedAt = DateTime.UtcNow;
            LastModifiedAt = DateTime.UtcNow;
        }
    }
}
