using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;
using System.Diagnostics;

namespace Social_Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly SocialDbContext _context;

        public HomeController(SocialDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            long currentUserId = long.Parse(userIdStr);
            var currentUser = await _context.Users.FindAsync(currentUserId);
            if (currentUser == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            // 1. Lấy toàn bộ bài viết cùng thông tin người đăng và các bình luận
            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // 2. Lấy ID của những người đã gửi/nhận kết bạn với người dùng hiện tại
            var relatedUserIds = await _context.Friendships
                .Where(f => f.RequestorId == currentUserId || f.ReceiverId == currentUserId)
                .Select(f => f.RequestorId == currentUserId ? f.ReceiverId : f.RequestorId)
                .ToListAsync();

            // Thêm chính mình vào danh sách loại trừ
            relatedUserIds.Add(currentUserId);

            // 3. Đề xuất kết bạn: Những tài khoản chưa có bất kỳ quan hệ kết bạn nào
            var recommendedUsers = await _context.Users
                .Where(u => !relatedUserIds.Contains(u.UserId))
                .Take(5)
                .ToListAsync();

            ViewBag.CurrentUser = currentUser;
            ViewBag.RecommendedUsers = recommendedUsers;

            return View(posts);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
