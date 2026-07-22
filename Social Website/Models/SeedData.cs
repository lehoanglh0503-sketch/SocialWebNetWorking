using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Social_Website.Models
{
    public static class SeedData
    {
        public static void SeedDatabase(SocialDbContext context)
        {
            context.Database.Migrate();

            if (!context.Users.Any())
            {
                // Mật khẩu hash cho "123456"
                string defaultPasswordHash = HashPassword("123456");

                var user1 = new User
                {
                    Username = "vietanh",
                    FullName = "Nguyễn Việt Anh",
                    PasswordHash = defaultPasswordHash,
                    AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=vietanh"
                };

                var user2 = new User
                {
                    Username = "lanhuong",
                    FullName = "Trần Thị Lan Hương",
                    PasswordHash = defaultPasswordHash,
                    AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=lanhuong"
                };

                var user3 = new User
                {
                    Username = "quanghuy",
                    FullName = "Lê Quang Huy",
                    PasswordHash = defaultPasswordHash,
                    AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=quanghuy"
                };

                context.Users.AddRange(user1, user2, user3);
                context.SaveChanges();

                // Tạo bài viết mẫu
                var post1 = new Post
                {
                    Content = "Chào mọi người! Đây là bài đăng đầu tiên của mình trên mạng xã hội này. Chúc mọi người một ngày làm việc thật hiệu quả! 🎉🚀",
                    CreatedAt = DateTime.Now.AddHours(-5),
                    UserId = user1.UserId
                };

                var post2 = new Post
                {
                    Content = "Hôm nay thời tiết đẹp quá, rất thích hợp để đi uống cà phê và lập trình ASP.NET Core! ☕💻",
                    CreatedAt = DateTime.Now.AddHours(-3),
                    UserId = user2.UserId
                };

                context.Posts.AddRange(post1, post2);
                context.SaveChanges();

                // Tạo bình luận mẫu
                var comment1 = new Comment
                {
                    Content = "Chào Việt Anh nhé! Chúc mừng mạng xã hội mới ra mắt.",
                    CreatedAt = DateTime.Now.AddHours(-4),
                    PostId = post1.PostId,
                    UserId = user2.UserId
                };

                var comment2 = new Comment
                {
                    Content = "Đúng vậy Hương ơi! Cho mình xin một slot đi chung với.",
                    CreatedAt = DateTime.Now.AddHours(-2),
                    PostId = post2.PostId,
                    UserId = user3.UserId
                };

                context.Comments.AddRange(comment1, comment2);
                context.SaveChanges();

                // Tạo kết bạn mẫu
                var friendship = new Friendship
                {
                    RequestorId = user1.UserId,
                    ReceiverId = user2.UserId,
                    IsAccepted = true
                };

                context.Friendships.Add(friendship);
                context.SaveChanges();
            }
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
