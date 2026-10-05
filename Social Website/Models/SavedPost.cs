using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class SavedPost
    {
        public long SavedPostId { get; set; }
        
        public long UserId { get; set; }
        public User? User { get; set; }
        
        public long PostId { get; set; }
        public Post? Post { get; set; }
        
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}
