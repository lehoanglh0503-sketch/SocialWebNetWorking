namespace Social_Website.Models
{
    public class ProfileViewModel
    {
        public User User { get; set; } = new();
        public int TotalPosts { get; set; }
        public int TotalComments { get; set; }
        public int TotalFriends { get; set; }
    }
}
