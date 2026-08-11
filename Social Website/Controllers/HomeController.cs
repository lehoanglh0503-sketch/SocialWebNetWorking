using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;
using Social_Website.Helpers;
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
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null || currentUser.IsLocked)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            if (currentUser.IsAdmin)
            {
                return RedirectToAction("Index", "Admin");
            }

            long currentUserId = currentUser.UserId;

            // 1. Lấy toàn bộ bài viết cùng thông tin người đăng, lượt thích, bình luận và phản hồi bình luận
            var posts = await _context.Posts
                .AsSplitQuery()
                .Include(p => p.User)
                .Include(p => p.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Likes)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.User)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Replies)
                        .ThenInclude(r => r.Likes)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            // 2. Lấy danh sách bạn bè đã kết bạn để chat
            var friends = await _context.Friendships
                .Where(f => (f.RequestorId == currentUserId || f.ReceiverId == currentUserId) && f.IsAccepted)
                .Select(f => f.RequestorId == currentUserId ? f.Receiver : f.Requestor)
                .ToListAsync();

            ViewBag.CurrentUser = currentUser;
            ViewBag.Friends = friends;

            return View(posts);
        }

        public IActionResult About()
        {
            return View();
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
