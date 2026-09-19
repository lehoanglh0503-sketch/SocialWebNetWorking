using System.Collections.Generic;

namespace Social_Website.Models
{
    public class SearchUserDto
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        
        // Trạng thái kết bạn: "None", "Friends", "PendingSent", "PendingReceived"
        public string FriendshipStatus { get; set; } = "None";
    }

    public class SearchViewModel
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchUserDto> Users { get; set; } = new List<SearchUserDto>();
        public List<Post> Posts { get; set; } = new List<Post>();
    }
}
