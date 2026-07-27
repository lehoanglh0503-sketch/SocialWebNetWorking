using System.Collections.Generic;

namespace Social_Website.Models
{
    public class AdminDashboardViewModel
    {
        public List<User> Users { get; set; } = new();
        public List<Post> Posts { get; set; } = new();
        public List<PostReport> Reports { get; set; } = new();
        
        public int TotalUsers { get; set; }
        public int TotalPosts { get; set; }
        public int TotalReports { get; set; }
    }
}
