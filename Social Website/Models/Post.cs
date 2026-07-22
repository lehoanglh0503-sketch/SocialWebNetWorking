using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class Post
    {
        public long PostId { get; set; }
        
        [Required(ErrorMessage = "Nội dung bài đăng không được để trống")]
        [StringLength(1000, ErrorMessage = "Nội dung bài đăng không quá 1000 ký tự")]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public long UserId { get; set; }
        public User? User { get; set; }
        
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<PostLike> Likes { get; set; } = new List<PostLike>();
    }
}
