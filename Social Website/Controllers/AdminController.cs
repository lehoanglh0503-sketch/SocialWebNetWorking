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

            // Xóa thủ công toàn bộ các ràng buộc khóa ngoại trước để tránh lỗi do Delete Behavior Restrict
            
            // 1. Xóa tất cả các lượt kết bạn mà user tham gia
            var friendships = _context.Friendships.Where(f => f.RequestorId == id || f.ReceiverId == id);
            _context.Friendships.RemoveRange(friendships);

            // 2. Xóa các lượt thích (likes) mà user đã thả tim ở các bài viết khác
            var likes = _context.PostLikes.Where(l => l.UserId == id);
            _context.PostLikes.RemoveRange(likes);

            // 3. Xóa các báo cáo do user này tạo ra
            var reportsCreated = _context.PostReports.Where(r => r.ReporterId == id);
            _context.PostReports.RemoveRange(reportsCreated);

            // 4. Xóa tất cả bình luận do user viết
            var comments = _context.Comments.Where(c => c.UserId == id);
            _context.Comments.RemoveRange(comments);

            // 5. Xóa tất cả bài viết của user này (kéo theo xóa bình luận, lượt thích, báo cáo trên bài viết này bằng Cascade)
            var posts = _context.Posts.Where(p => p.UserId == id);
            _context.Posts.RemoveRange(posts);

            // 6. Cuối cùng xóa User
            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa người dùng thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeletePost(long id)
        {
            if (!this.IsAdmin()) return Json(new { success = false, message = "Không có quyền truy cập" });

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại hoặc đã bị xóa" });
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa bài viết thành công!" });
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
