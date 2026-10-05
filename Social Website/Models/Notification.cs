using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class Notification
    {
        public long NotificationId { get; set; }
        
        // Người nhận thông báo
        public long UserId { get; set; }
        public User? User { get; set; }
        
        // Người thực hiện hành động (ví dụ: người like, comment, tag, kết bạn)
        public long ActorId { get; set; }
        public User? Actor { get; set; }
        
        // Loại thông báo: LikePost, LikeComment, CommentPost, ReplyComment, TagPost, TagComment, FriendRequest, FriendAccept
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;
        
        // Nội dung hiển thị thông báo
        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;
        
        // Đã đọc hay chưa
        public bool IsRead { get; set; } = false;
        
        // Đường dẫn khi nhấp vào thông báo
        public string? TargetUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
