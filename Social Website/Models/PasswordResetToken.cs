using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class PasswordResetToken
    {
        public long PasswordResetTokenId { get; set; }
        
        public long UserId { get; set; }
        public User? User { get; set; }
        
        [Required]
        [StringLength(256)]
        public string TokenHash { get; set; } = string.Empty;
        
        public DateTime ExpiresAt { get; set; }
        
        public DateTime? UsedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
