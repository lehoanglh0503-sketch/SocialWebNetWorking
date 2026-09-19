using System;
using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class Message
    {
        public long MessageId { get; set; }

        public long SenderId { get; set; }
        public User? Sender { get; set; }

        public long ReceiverId { get; set; }
        public User? Receiver { get; set; }

        public string? Content { get; set; }
        
        public string? ImageUrl { get; set; }

        public long? SharedPostId { get; set; }
        public Post? SharedPost { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
