using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;
using Social_Website.Helpers;

namespace Social_Website.Controllers
{
    public class AdminController : Controller
    {
        private readonly SocialDbContext _context;

        public AdminController(SocialDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!this.IsAdmin())
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.CurrentUserId = this.GetCurrentUserId() ?? 0;

            var viewModel = new AdminDashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalPosts = await _context.Posts.CountAsync(),
                TotalReports = await _context.PostReports.CountAsync(),

                Users = await _context.Users.ToListAsync(),
                Posts = await _context.Posts
                    .Include(p => p.User)
                    .Include(p => p.Likes)
                    .Include(p => p.Comments)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(),
                Reports = await _context.PostReports
                    .Include(r => r.Reporter)
                    .Include(r => r.Post)
                        .ThenInclude(p => p!.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Users()
        {
            if (!this.IsAdmin())
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.CurrentUserId = this.GetCurrentUserId() ?? 0;

            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeUserPassword(long id, string newPassword)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu mới phải từ 6 ký tự trở lên" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            user.PasswordHash = SeedData.HashPassword(newPassword);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã thay đổi mật khẩu cho người dùng \"{user.FullName}\" thành công!" });
        }

        private static void DeletePhysicalFile(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            try
            {
                var relativePath = relativeUrl.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch { }
        }

        private async Task DeleteSinglePostInternalAsync(Post post)
        {
            long postId = post.PostId;

            // 1. Gỡ SharedPostId ở Messages
            var sharedMessages = await _context.Messages
                .Where(m => m.SharedPostId == postId)
                .ToListAsync();
            foreach (var m in sharedMessages)
            {
                m.SharedPostId = null;
            }

            // 2. Xóa SavedPosts liên quan
            var savedPosts = await _context.SavedPosts
                .Where(sp => sp.PostId == postId)
                .ToListAsync();
            _context.SavedPosts.RemoveRange(savedPosts);

            // 3. Xóa Báo cáo liên quan
            var reports = await _context.PostReports
                .Where(pr => pr.PostId == postId)
                .ToListAsync();
            _context.PostReports.RemoveRange(reports);

            // 4. Xóa PostLikes liên quan
            var likes = await _context.PostLikes
                .Where(pl => pl.PostId == postId)
                .ToListAsync();
            _context.PostLikes.RemoveRange(likes);

            // 5. Xử lý Bình luận và Trả lời bình luận trên bài viết
            var postComments = await _context.Comments
                .Where(c => c.PostId == postId)
                .ToListAsync();
            var commentIds = postComments.Select(c => c.CommentId).ToList();

            if (commentIds.Any())
            {
                // Ngắt liên kết ParentCommentId cho các phản hồi ngoài (nếu có)
                var externalReplies = await _context.Comments
                    .Where(c => c.ParentCommentId.HasValue &&
                                commentIds.Contains(c.ParentCommentId.Value) &&
                                !commentIds.Contains(c.CommentId))
                    .ToListAsync();
                foreach (var r in externalReplies)
                {
                    r.ParentCommentId = null;
                }

                // Gỡ ParentCommentId của chính các bình luận trong bài viết để tránh lỗi Restrict
                foreach (var c in postComments)
                {
                    c.ParentCommentId = null;
                }
                await _context.SaveChangesAsync();

                // Xóa CommentLikes
                var commentLikes = await _context.CommentLikes
                    .Where(cl => commentIds.Contains(cl.CommentId))
                    .ToListAsync();
                _context.CommentLikes.RemoveRange(commentLikes);

                // Xóa Comments
                _context.Comments.RemoveRange(postComments);
                await _context.SaveChangesAsync();
            }

            // 6. Xóa PostImages và file vật lý
            var postImages = await _context.PostImages
                .Where(pi => pi.PostId == postId)
                .ToListAsync();
            foreach (var img in postImages)
            {
                DeletePhysicalFile(img.ImageUrl);
            }
            _context.PostImages.RemoveRange(postImages);

            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                foreach (var url in post.ImageUrl.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    DeletePhysicalFile(url);
                }
            }
            if (!string.IsNullOrEmpty(post.VideoUrl))
            {
                DeletePhysicalFile(post.VideoUrl);
            }

            // 7. Xóa bài viết
            _context.Posts.Remove(post);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(long id)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            long currentUserId = this.GetCurrentUserId() ?? 0;

            if (currentUserId == id)
            {
                return Json(new { success = false, message = "Bạn không thể tự xóa tài khoản của chính mình!" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Xóa tất cả các lượt kết bạn mà user tham gia
                var friendships = await _context.Friendships
                    .Where(f => f.RequestorId == id || f.ReceiverId == id)
                    .ToListAsync();
                _context.Friendships.RemoveRange(friendships);

                // 2. Xóa tất cả tin nhắn liên quan (gửi hoặc nhận) cùng file đính kèm
                var messages = await _context.Messages
                    .Where(m => m.SenderId == id || m.ReceiverId == id)
                    .ToListAsync();
                foreach (var msg in messages)
                {
                    DeletePhysicalFile(msg.ImageUrl);
                }
                _context.Messages.RemoveRange(messages);

                // 3. Xóa tất cả thông báo mà user nhận hoặc là người thực hiện (Actor)
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == id || n.ActorId == id)
                    .ToListAsync();
                _context.Notifications.RemoveRange(notifications);

                // 4. Xóa token khôi phục mật khẩu của người dùng
                var resetTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserId == id)
                    .ToListAsync();
                _context.PasswordResetTokens.RemoveRange(resetTokens);

                // 5. Xóa danh sách bài viết đã lưu của user
                var savedPosts = await _context.SavedPosts
                    .Where(sp => sp.UserId == id)
                    .ToListAsync();
                _context.SavedPosts.RemoveRange(savedPosts);

                // 6. Xóa các báo cáo do user này tạo ra
                var reportsCreated = await _context.PostReports
                    .Where(r => r.ReporterId == id)
                    .ToListAsync();
                _context.PostReports.RemoveRange(reportsCreated);

                // 7. Xóa tất cả bài viết do user này đăng tải (dọn sạch bình luận, ảnh, lượt thích, báo cáo của từng bài)
                var userPosts = await _context.Posts
                    .Include(p => p.Images)
                    .Where(p => p.UserId == id)
                    .ToListAsync();

                foreach (var post in userPosts)
                {
                    await DeleteSinglePostInternalAsync(post);
                }
                await _context.SaveChangesAsync();

                // 8. Xóa các lượt thích bài viết (PostLikes) mà user đã thả ở các bài viết của người khác
                var remainingPostLikes = await _context.PostLikes
                    .Where(l => l.UserId == id)
                    .ToListAsync();
                _context.PostLikes.RemoveRange(remainingPostLikes);

                // 9. Xử lý các bình luận do user viết trên bài viết của người khác
                var userComments = await _context.Comments
                    .Where(c => c.UserId == id)
                    .ToListAsync();
                var userCommentIds = userComments.Select(c => c.CommentId).ToList();

                if (userCommentIds.Any())
                {
                    // Ngắt liên kết ParentCommentId cho các câu trả lời của người khác tới bình luận của user này
                    var externalReplies = await _context.Comments
                        .Where(c => c.ParentCommentId.HasValue &&
                                    userCommentIds.Contains(c.ParentCommentId.Value) &&
                                    !userCommentIds.Contains(c.CommentId))
                        .ToListAsync();
                    foreach (var r in externalReplies)
                    {
                        r.ParentCommentId = null;
                    }

                    // Gỡ ParentCommentId của chính các bình luận do user viết
                    foreach (var c in userComments)
                    {
                        c.ParentCommentId = null;
                    }
                    await _context.SaveChangesAsync();

                    // Xóa các lượt thích trên các bình luận này
                    var commentLikesOnUserComments = await _context.CommentLikes
                        .Where(cl => userCommentIds.Contains(cl.CommentId))
                        .ToListAsync();
                    _context.CommentLikes.RemoveRange(commentLikesOnUserComments);

                    // Xóa các bình luận này
                    _context.Comments.RemoveRange(userComments);
                    await _context.SaveChangesAsync();
                }

                // 10. Xóa các lượt thích bình luận (CommentLikes) mà user đã thả tim ở các bình luận khác
                var userCommentLikes = await _context.CommentLikes
                    .Where(cl => cl.UserId == id)
                    .ToListAsync();
                _context.CommentLikes.RemoveRange(userCommentLikes);

                // 11. Xóa ảnh đại diện vật lý nếu là file tải lên (/uploads/...)
                if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
                {
                    DeletePhysicalFile(user.AvatarUrl);
                }

                // 12. Cuối cùng xóa User
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { success = true, message = $"Đã xóa tài khoản \"{user.FullName}\" cùng toàn bộ dữ liệu thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Không thể xóa tài khoản do lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(long id)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            var post = await _context.Posts
                .Include(p => p.Images)
                .FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại hoặc đã bị xóa" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await DeleteSinglePostInternalAsync(post);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã xóa bài viết thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi khi xóa bài viết: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DismissReport(long id)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            var report = await _context.PostReports.FindAsync(id);
            if (report == null)
            {
                return Json(new { success = false, message = "Báo cáo không tồn tại" });
            }

            _context.PostReports.Remove(report);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã bỏ qua báo cáo thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> ApproveReport(long id)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            var report = await _context.PostReports
                .Include(r => r.Post)
                    .ThenInclude(p => p!.User)
                .Include(r => r.Post)
                    .ThenInclude(p => p!.Images)
                .FirstOrDefaultAsync(r => r.PostReportId == id);

            if (report == null)
            {
                return Json(new { success = false, message = "Báo cáo không tồn tại" });
            }

            var post = report.Post;
            string messageExtra = "";

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (post != null && post.User != null)
                {
                    var author = post.User;
                    author.ApprovedReportCount += 1;

                    if (author.ApprovedReportCount >= 3)
                    {
                        author.IsLocked = true;
                        author.LockReason = "AutoViolation";
                        messageExtra = $" Tài khoản \"{author.FullName}\" đã đạt {author.ApprovedReportCount} lần vi phạm và ĐÃ BỊ KHÓA!";
                    }
                    else
                    {
                        messageExtra = $" Tài khoản \"{author.FullName}\" hiện đã có {author.ApprovedReportCount} lần vi phạm (Sẽ bị khóa nếu đạt 3 lần).";
                    }

                    await DeleteSinglePostInternalAsync(post);
                }
                else
                {
                    _context.PostReports.Remove(report);
                    messageExtra = " Đã dọn dẹp báo cáo.";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã duyệt báo cáo và xử lý bài viết!" + messageExtra });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Lỗi khi xử lý duyệt báo cáo: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLockUser(long id)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            long currentUserId = this.GetCurrentUserId() ?? 0;
            if (currentUserId == id)
            {
                return Json(new { success = false, message = "Bạn không thể tự thao tác với tài khoản của chính mình!" });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            user.IsLocked = !user.IsLocked;
            if (user.IsLocked)
            {
                user.LockReason = "Manual";
            }
            else
            {
                user.LockReason = null;
            }

            await _context.SaveChangesAsync();

            string statusText = user.IsLocked ? "đã bị khóa" : "đã được mở khóa";
            return Json(new { success = true, message = $"Tài khoản \"{user.FullName}\" {statusText} thành công!", isLocked = user.IsLocked });
        }

        public async Task<IActionResult> BannedWords()
        {
            if (!this.IsAdmin())
            {
                return RedirectToAction("Login", "Auth");
            }

            ViewBag.CurrentUserId = this.GetCurrentUserId() ?? 0;

            var words = await _context.BannedWords.OrderBy(w => w.Word).ToListAsync();
            return View(words);
        }

        [HttpPost]
        public async Task<IActionResult> AddBannedWord(string word)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            if (string.IsNullOrEmpty(word) || word.Trim().Length == 0)
            {
                return Json(new { success = false, message = "Từ ngữ không được rỗng" });
            }

            string cleanWord = word.Trim().ToLower();

            // Kiểm tra trùng lặp
            var exists = await _context.BannedWords.AnyAsync(w => w.Word.ToLower() == cleanWord);
            if (exists)
            {
                return Json(new { success = false, message = "Từ cấm này đã tồn tại trong hệ thống" });
            }

            var bannedWord = new BannedWord
            {
                Word = word.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.BannedWords.Add(bannedWord);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm từ cấm mới thành công!", bannedWordId = bannedWord.BannedWordId, word = bannedWord.Word });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteBannedWord(long id)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            var bannedWord = await _context.BannedWords.FindAsync(id);
            if (bannedWord == null)
            {
                return Json(new { success = false, message = "Không tìm thấy từ cấm" });
            }

            _context.BannedWords.Remove(bannedWord);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa từ cấm thành công!" });
        }
    }
}
