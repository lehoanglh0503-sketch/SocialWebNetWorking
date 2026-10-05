using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Helpers;
using Social_Website.Models;

namespace Social_Website.Controllers
{
    public class NotificationController : Controller
    {
        private readonly SocialDbContext _context;

        public NotificationController(SocialDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications(int count = 20)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.UserId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .Include(n => n.Actor)
                .Select(n => new
                {
                    notificationId = n.NotificationId,
                    type = n.Type,
                    message = n.Message,
                    isRead = n.IsRead,
                    targetUrl = n.TargetUrl,
                    createdAt = n.CreatedAt.ToVietnamTimeString(),
                    timeAgo = GetTimeAgo(n.CreatedAt),
                    actor = new
                    {
                        userId = n.ActorId,
                        fullName = n.Actor != null ? n.Actor.FullName : "Người dùng",
                        avatarUrl = n.Actor != null ? n.Actor.AvatarUrl : "/uploads/default-avatar.png",
                        username = n.Actor != null ? n.Actor.Username : ""
                    }
                })
                .ToListAsync();

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead);

            return Json(new { success = true, notifications = notifications, unreadCount = unreadCount });
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, unreadCount = 0 });
            }

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead);

            return Json(new { success = true, unreadCount = unreadCount });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == id && n.UserId == currentUser.UserId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            int unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == currentUser.UserId && !n.IsRead);

            return Json(new { success = true, unreadCount = unreadCount });
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == currentUser.UserId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, unreadCount = 0 });
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var vnNow = DateTime.UtcNow.ToVietnamTime();
            var vnTarget = dateTime.ToVietnamTime();
            var timeSpan = vnNow - vnTarget;

            if (timeSpan.TotalSeconds < 60)
                return "Vừa xong";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} phút trước";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} giờ trước";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} ngày trước";

            return vnTarget.ToString("dd/MM/yyyy");
        }
    }
}
