using System;
using System.ComponentModel.DataAnnotations;

namespace OnlyOfficeServerFramework.Models
{
    public class Installation
    {
        public Guid Id { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        [Required]
        [MaxLength(500)]
        public string BaseUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Installation()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
