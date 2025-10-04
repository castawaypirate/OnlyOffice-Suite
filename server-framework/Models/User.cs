using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OnlyOfficeServerFramework.Models
{
    public class User
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; }

        [Required]
        [MaxLength(255)]
        public string Password { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation property
        public virtual ICollection<FileEntity> Files { get; set; }

        public User()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Files = new List<FileEntity>();
        }
    }
}
