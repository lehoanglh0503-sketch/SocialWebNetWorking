using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;

namespace Social_Website.Controllers
{
    public class FriendController : Controller
    {
        private readonly SocialDbContext _context;

        public FriendController(SocialDbContext context)
        {
            _context = context;
        }

        private User? GetCurrentUser()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return null;

            long userId = long.Parse(userIdStr);
            return _context.Users.Find(userId);
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            long currentUserId = currentUser.UserId;

            // 1. Danh sách bạn bè hiện tại (đã kết bạn)
            var friends = await _context.Friendships
                .Where(f => (f.RequestorId == currentUserId || f.ReceiverId == currentUserId) && f.IsAccepted)
                .Select(f => f.RequestorId == currentUserId ? f.Receiver : f.Requestor)
                .ToListAsync();

            // 2. Yêu cầu kết bạn đang chờ (người khác gửi đến)
            var pendingRequests = await _context.Friendships
                .Where(f => f.ReceiverId == currentUserId && !f.IsAccepted)
                .Include(f => f.Requestor)
                .ToListAsync();

            // 3. Yêu cầu kết bạn đã gửi (chưa chấp nhận)
            var sentRequests = await _context.Friendships
                .Where(f => f.RequestorId == currentUserId && !f.IsAccepted)
                .Select(f => f.ReceiverId)
                .ToListAsync();

            ViewBag.Friends = friends;
            ViewBag.PendingRequests = pendingRequests;
            ViewBag.SentRequests = sentRequests;
            ViewBag.CurrentUser = currentUser;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(long receiverId)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            long requestorId = currentUser.UserId;
            if (requestorId == receiverId) return Json(new { success = false, message = "Không thể kết bạn với chính mình" });

            // Kiểm tra xem đã tồn tại mối quan hệ nào chưa
            var existing = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.RequestorId == requestorId && f.ReceiverId == receiverId) ||
                                          (f.RequestorId == receiverId && f.ReceiverId == requestorId));

            if (existing != null)
            {
                return Json(new { success = false, message = "Yêu cầu hoặc quan hệ đã tồn tại" });
            }

            var friendship = new Friendship
            {
                RequestorId = requestorId,
                ReceiverId = receiverId,
                IsAccepted = false
            };

            _context.Friendships.Add(friendship);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã gửi lời mời kết bạn" });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(long requestorId)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            long receiverId = currentUser.UserId;

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequestorId == requestorId && f.ReceiverId == receiverId && !f.IsAccepted);

            if (friendship == null)
            {
                return Json(new { success = false, message = "Không tìm thấy yêu cầu kết bạn" });
            }

            friendship.IsAccepted = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã chấp nhận kết bạn" });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriend(long friendId)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });

            long currentUserId = currentUser.UserId;

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => (f.RequestorId == currentUserId && f.ReceiverId == friendId) ||
                                          (f.RequestorId == friendId && f.ReceiverId == currentUserId));

            if (friendship == null)
            {
                return Json(new { success = false, message = "Không tìm thấy quan hệ bạn bè" });
            }

            _context.Friendships.Remove(friendship);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã hủy kết bạn/hủy yêu cầu" });
        }
    }
}
