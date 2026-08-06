using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class Comment
    {
        public long CommentId { get; set; }
        
        [Required(ErrorMessage = "Bình luận không được để trống")]
        [StringLength(500, ErrorMessage = "Nội dung bình luận không quá 500 ký tự")]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        public long PostId { get; set; }
        public Post? Post { get; set; }
        
        public long UserId { get; set; }
        public User? User { get; set; }

        // Phản hồi / Trả lời bình luận
        public long? ParentCommentId { get; set; }
        public Comment? ParentComment { get; set; }
        public List<Comment> Replies { get; set; } = new();

        // Tương tác thả cảm xúc / Thích bình luận
        public List<CommentLike> Likes { get; set; } = new();
    }
}
