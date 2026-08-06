using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class CommentLike
    {
        [Key]
        public long LikeId { get; set; }
        
        public long CommentId { get; set; }
        public Comment? Comment { get; set; }
        
        public long UserId { get; set; }
        public User? User { get; set; }
        
        public string ReactionType { get; set; } = "Like";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
