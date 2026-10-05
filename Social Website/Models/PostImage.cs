using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class PostImage
    {
        public long PostImageId { get; set; }
        
        public long PostId { get; set; }
        public Post? Post { get; set; }
        
        [Required]
        public string ImageUrl { get; set; } = string.Empty;
        
        public int OrderIndex { get; set; } = 0;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
