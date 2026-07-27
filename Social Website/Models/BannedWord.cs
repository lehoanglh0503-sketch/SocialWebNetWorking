using System;
using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class BannedWord
    {
        public long BannedWordId { get; set; }

        [Required]
        [StringLength(100)]
        public string Word { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
