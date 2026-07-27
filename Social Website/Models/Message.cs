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

        [Required(ErrorMessage = "Nội dung tin nhắn không được rỗng")]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
