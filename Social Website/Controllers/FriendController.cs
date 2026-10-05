using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Social_Website.Hubs;
using Social_Website.Models;
using Social_Website.Helpers;

namespace Social_Website.Controllers
{
    public class FriendController : Controller
    {
        private readonly SocialDbContext _context;
        private readonly IHubContext<SocialHub> _hubContext;

        public FriendController(SocialDbContext context, IHubContext<SocialHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> Index()
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (currentUser.IsAdmin)
            {
                return RedirectToAction("Index", "Admin");
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
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });
            if (currentUser.IsAdmin) return Json(new { success = false, message = "Tài khoản quản trị không thể kết bạn" });

            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null) return Json(new { success = false, message = "Người dùng không tồn tại" });
            if (receiver.IsAdmin) return Json(new { success = false, message = "Không thể kết bạn với tài khoản quản trị" });

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

            // Tạo thông báo kết bạn
            var notification = new Notification
            {
                UserId = receiverId,
                ActorId = requestorId,
                Type = "FriendRequest",
                Message = $"{currentUser.FullName} đã gửi cho bạn lời mời kết bạn.",
                TargetUrl = "/Friend/Index",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast tới người nhận qua SignalR
            await _hubContext.Clients.Group($"User_{receiverId}").SendAsync("ReceiveNotification", new
            {
                notificationId = notification.NotificationId,
                type = notification.Type,
                message = notification.Message,
                isRead = notification.IsRead,
                targetUrl = notification.TargetUrl,
                createdAt = notification.CreatedAt.ToVietnamTimeString(),
                timeAgo = "Vừa xong",
                actor = new
                {
                    userId = currentUser.UserId,
                    fullName = currentUser.FullName,
                    avatarUrl = currentUser.AvatarUrl,
                    username = currentUser.Username
                }
            });

            return Json(new { success = true, message = "Đã gửi lời mời kết bạn" });
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(long requestorId)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });
            if (currentUser.IsAdmin) return Json(new { success = false, message = "Tài khoản quản trị không thể thực hiện hành động này" });

            long receiverId = currentUser.UserId;

            var friendship = await _context.Friendships
                .FirstOrDefaultAsync(f => f.RequestorId == requestorId && f.ReceiverId == receiverId && !f.IsAccepted);

            if (friendship == null)
            {
                return Json(new { success = false, message = "Không tìm thấy yêu cầu kết bạn" });
            }

            friendship.IsAccepted = true;

            // Tạo thông báo khi nhận kết bạn
            var notification = new Notification
            {
                UserId = requestorId,
                ActorId = receiverId,
                Type = "FriendAccept",
                Message = $"{currentUser.FullName} đã chấp nhận lời mời kết bạn của bạn.",
                TargetUrl = $"/Profile/Index?userId={receiverId}",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Broadcast tới người gửi yêu cầu kết bạn
            await _hubContext.Clients.Group($"User_{requestorId}").SendAsync("ReceiveNotification", new
            {
                notificationId = notification.NotificationId,
                type = notification.Type,
                message = notification.Message,
                isRead = notification.IsRead,
                targetUrl = notification.TargetUrl,
                createdAt = notification.CreatedAt.ToVietnamTimeString(),
                timeAgo = "Vừa xong",
                actor = new
                {
                    userId = currentUser.UserId,
                    fullName = currentUser.FullName,
                    avatarUrl = currentUser.AvatarUrl,
                    username = currentUser.Username
                }
            });

            return Json(new { success = true, message = "Đã chấp nhận kết bạn" });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriend(long friendId)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null) return Json(new { success = false, message = "Vui lòng đăng nhập" });
            if (currentUser.IsAdmin) return Json(new { success = false, message = "Tài khoản quản trị không thể thực hiện hành động này" });

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
