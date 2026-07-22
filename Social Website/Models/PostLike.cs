namespace Social_Website.Models
{
    public class PostLike
    {
        public long PostLikeId { get; set; }
        
        public long PostId { get; set; }
        public Post? Post { get; set; }
        
        public long UserId { get; set; }
        public User? User { get; set; }
    }
}
