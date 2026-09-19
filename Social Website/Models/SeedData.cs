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

            // Đảm bảo cột ImageUrl và SharedPostId tồn tại trong bảng Messages
            try
            {
                context.Database.ExecuteSqlRaw(@"
                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Messages]') AND name = 'ImageUrl')
                    BEGIN
                        ALTER TABLE [Messages] ADD [ImageUrl] nvarchar(max) NULL;
                    END

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Messages]') AND name = 'SharedPostId')
                    BEGIN
                        ALTER TABLE [Messages] ADD [SharedPostId] bigint NULL;
                        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Messages_SharedPostId' AND object_id = OBJECT_ID(N'[Messages]'))
                        BEGIN
                            CREATE INDEX [IX_Messages_SharedPostId] ON [Messages] ([SharedPostId]);
                        END
                        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Messages_Posts_SharedPostId')
                        BEGIN
                            ALTER TABLE [Messages] ADD CONSTRAINT [FK_Messages_Posts_SharedPostId] FOREIGN KEY ([SharedPostId]) REFERENCES [Posts] ([PostId]) ON DELETE SET NULL;
                        END
                    END

                    IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260920000000_AddImageAndSharedPostToMessage')
                    BEGIN
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES (N'20260920000000_AddImageAndSharedPostToMessage', N'6.0.25');
                    END
                ");
            }
            catch { }

            // Đảm bảo có dữ liệu từ cấm mặc định
            if (!context.BannedWords.Any())
            {
                var defaultWords = new List<BannedWord>
                {
                    new BannedWord { Word = "giết" },
                    new BannedWord { Word = "chém" },
                    new BannedWord { Word = "đâm" },
                    new BannedWord { Word = "đánh" },
                    new BannedWord { Word = "tự tử" },
                    new BannedWord { Word = "tự sát" },
                    new BannedWord { Word = "bạo lực" },
                    new BannedWord { Word = "côn đồ" },
                    new BannedWord { Word = "hành hung" },
                    new BannedWord { Word = "đánh đập" },
                    new BannedWord { Word = "tra tấn" },
                    new BannedWord { Word = "khủng bố" },
                    new BannedWord { Word = "kích động" }
                };
                context.BannedWords.AddRange(defaultWords);
                context.SaveChanges();
            }

            // Đảm bảo có tài khoản Admin
            var existingAdmin = context.Users.FirstOrDefault(u => u.Username == "admin");
            if (existingAdmin != null)
            {
                if (!existingAdmin.IsAdmin)
                {
                    existingAdmin.IsAdmin = true;
                    existingAdmin.PasswordHash = HashPassword("admin123");
                    context.SaveChanges();
                }
            }
            else
            {
                var userAdmin = new User
                {
                    Username = "admin",
                    FullName = "Quản trị viên",
                    PasswordHash = HashPassword("admin123"),
                    AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=admin",
                    IsAdmin = true
                };
                context.Users.Add(userAdmin);
                context.SaveChanges();
            }

            // Làm mới toàn bộ bài viết, bình luận, tin nhắn, tương tác và người dùng cũ
            context.Messages.RemoveRange(context.Messages);
            context.PostReports.RemoveRange(context.PostReports);
            context.CommentLikes.RemoveRange(context.CommentLikes);
            context.Comments.RemoveRange(context.Comments);
            context.PostLikes.RemoveRange(context.PostLikes);
            context.Posts.RemoveRange(context.Posts);
            context.Friendships.RemoveRange(context.Friendships);
            
            var nonAdminUsers = context.Users.Where(u => !u.IsAdmin).ToList();
            context.Users.RemoveRange(nonAdminUsers);
            context.SaveChanges();

            // Mật khẩu mặc định cho tất cả người dùng: "123456"
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

            var user4 = new User
            {
                Username = "tantai",
                FullName = "Nguyễn Tấn Tài",
                PasswordHash = defaultPasswordHash,
                AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=tantai"
            };

            var user5 = new User
            {
                Username = "hanh",
                FullName = "Nguyễn Thị Mỹ Hạnh",
                PasswordHash = defaultPasswordHash,
                AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=hanh"
            };

            var user6 = new User
            {
                Username = "minhtuan",
                FullName = "Phạm Minh Tuấn",
                PasswordHash = defaultPasswordHash,
                AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=minhtuan"
            };

            var user7 = new User
            {
                Username = "phuongthao",
                FullName = "Hoàng Phương Thảo",
                PasswordHash = defaultPasswordHash,
                AvatarUrl = "https://api.dicebear.com/7.x/adventurer/svg?seed=phuongthao"
            };

            context.Users.AddRange(user1, user2, user3, user4, user5, user6, user7);
            context.SaveChanges();

            // Tạo bài viết mẫu (có ảnh & không ảnh)
            var post1 = new Post
            {
                Content = "Hôm nay hoàn thành xong dự án mạng xã hội Connectify với ASP.NET Core và SignalR! Giao diện siêu mượt và tính năng chat thời gian thực cực kỳ xịn xò 💻🚀🎉",
                ImageUrl = "https://images.unsplash.com/photo-1498050108023-c5249f4df085?w=800",
                CreatedAt = DateTime.Now.AddHours(-12),
                UserId = user1.UserId
            };

            var post2 = new Post
            {
                Content = "Thứ 7 thảnh thơi tại quán cà phê quen thuộc. Thưởng thức một ly Latte nóng và đọc một cuốn sách hay ☕📖✨",
                ImageUrl = "https://images.unsplash.com/photo-1501339847302-ac426a4a7cbb?w=800",
                CreatedAt = DateTime.Now.AddHours(-10),
                UserId = user2.UserId
            };

            var post3 = new Post
            {
                Content = "Cuối tuần rồi mọi người ơi! Có ai đi đá bóng hay đi cafe ở khu vực Quận 1 không, lập kèo đi chung nào? ⚽🍻",
                ImageUrl = null,
                CreatedAt = DateTime.Now.AddHours(-8),
                UserId = user3.UserId
            };

            var post4 = new Post
            {
                Content = "Chuyến du lịch biển đợt này thật là tuyệt vời! Nắng vàng, biển xanh và không khí cực kỳ trong lành 🌊☀️🏖️",
                ImageUrl = "https://images.unsplash.com/photo-1507525428034-b723cf961d3e?w=800",
                CreatedAt = DateTime.Now.AddHours(-6),
                UserId = user4.UserId
            };

            var post5 = new Post
            {
                Content = "Mỗi ngày học thêm một điều mới là một bước tiến gần hơn tới mục tiêu của bản thân. Hãy cố gắng hết mình nhé các bạn! 💪📚",
                ImageUrl = null,
                CreatedAt = DateTime.Now.AddHours(-4),
                UserId = user5.UserId
            };

            var post6 = new Post
            {
                Content = "Setup góc làm việc mới tối giản cho tuần làm việc tràn đầy năng lượng 🖥️✨",
                ImageUrl = "https://images.unsplash.com/photo-1517694712202-14dd9538aa97?w=800",
                CreatedAt = DateTime.Now.AddHours(-2),
                UserId = user6.UserId
            };

            var post7 = new Post
            {
                Content = "Thời tiết dạo này dễ chịu thật đấy, chiều đi dạo quanh công viên ngắm hoàng hôn thôi 🌅🍃",
                ImageUrl = null,
                CreatedAt = DateTime.Now.AddMinutes(-30),
                UserId = user7.UserId
            };

            context.Posts.AddRange(post1, post2, post3, post4, post5, post6, post7);
            context.SaveChanges();

            // Thêm lượt thích mẫu
            context.PostLikes.AddRange(
                new PostLike { PostId = post1.PostId, UserId = user2.UserId, ReactionType = "Love" },
                new PostLike { PostId = post1.PostId, UserId = user3.UserId, ReactionType = "Like" },
                new PostLike { PostId = post1.PostId, UserId = user4.UserId, ReactionType = "Wow" },
                new PostLike { PostId = post2.PostId, UserId = user1.UserId, ReactionType = "Love" },
                new PostLike { PostId = post2.PostId, UserId = user5.UserId, ReactionType = "Haha" },
                new PostLike { PostId = post4.PostId, UserId = user1.UserId, ReactionType = "Love" },
                new PostLike { PostId = post4.PostId, UserId = user2.UserId, ReactionType = "Wow" },
                new PostLike { PostId = post6.PostId, UserId = user1.UserId, ReactionType = "Like" }
            );

            // Thêm bình luận mẫu
            context.Comments.AddRange(
                new Comment { PostId = post1.PostId, UserId = user2.UserId, Content = "Chúc mừng Việt Anh nha! Dự án xịn quá 👏", CreatedAt = DateTime.Now.AddHours(-11) },
                new Comment { PostId = post1.PostId, UserId = user3.UserId, Content = "Quá đỉnh luôn ông ơi!", CreatedAt = DateTime.Now.AddHours(-10) },
                new Comment { PostId = post2.PostId, UserId = user1.UserId, Content = "Quán cafe ở đâu vậy Hương ơi, nhìn chill quá!", CreatedAt = DateTime.Now.AddHours(-9) },
                new Comment { PostId = post3.PostId, UserId = user4.UserId, Content = "Cho tui tham gia kèo cafe với nha Huy!", CreatedAt = DateTime.Now.AddHours(-7) },
                new Comment { PostId = post4.PostId, UserId = user7.UserId, Content = "Cảnh đẹp xuất sắc luôn Tài ơi 😍", CreatedAt = DateTime.Now.AddHours(-5) }
            );

            // Thêm quan hệ bạn bè mẫu
            context.Friendships.AddRange(
                new Friendship { RequestorId = user1.UserId, ReceiverId = user2.UserId, IsAccepted = true },
                new Friendship { RequestorId = user1.UserId, ReceiverId = user3.UserId, IsAccepted = true },
                new Friendship { RequestorId = user1.UserId, ReceiverId = user4.UserId, IsAccepted = true },
                new Friendship { RequestorId = user2.UserId, ReceiverId = user5.UserId, IsAccepted = true },
                new Friendship { RequestorId = user3.UserId, ReceiverId = user6.UserId, IsAccepted = true },
                new Friendship { RequestorId = user1.UserId, ReceiverId = user7.UserId, IsAccepted = false }
            );

            context.SaveChanges();
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
