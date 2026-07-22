namespace Social_Website.Models
{
    public class Friendship
    {
        public long FriendshipId { get; set; }
        
        public long RequestorId { get; set; }
        public User? Requestor { get; set; }
        
        public long ReceiverId { get; set; }
        public User? Receiver { get; set; }
        
        public bool IsAccepted { get; set; }
    }
}
