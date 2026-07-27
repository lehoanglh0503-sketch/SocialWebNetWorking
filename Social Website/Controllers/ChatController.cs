using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Social_Website.Hubs;
using Social_Website.Models;
using Social_Website.Helpers;

namespace Social_Website.Controllers
{
    public class ChatController : Controller
    {
        private readonly SocialDbContext _context;
        private readonly IHubContext<SocialHub> _hubContext;

        public ChatController(SocialDbContext context, IHubContext<SocialHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(long friendId)
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            // Verify they are friends
            var isFriend = await _context.Friendships.AnyAsync(f =>
                ((f.RequestorId == currentUserId.Value && f.ReceiverId == friendId) ||
                 (f.RequestorId == friendId && f.ReceiverId == currentUserId.Value)) && f.IsAccepted);

            if (!isFriend)
            {
                return Json(new { success = false, message = "Bạn chỉ có thể xem tin nhắn với bạn bè" });
            }

            var messages = await _context.Messages
                .Where(m => (m.SenderId == currentUserId.Value && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == currentUserId.Value))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    messageId = m.MessageId,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    createdAt = m.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(new { success = true, messages = messages });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(long receiverId, string content)
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrEmpty(content) || content.Trim().Length == 0)
            {
                return Json(new { success = false, message = "Tin nhắn không được để trống" });
            }

            // Verify they are friends
            var isFriend = await _context.Friendships.AnyAsync(f =>
                ((f.RequestorId == currentUserId.Value && f.ReceiverId == receiverId) ||
                 (f.RequestorId == receiverId && f.ReceiverId == currentUserId.Value)) && f.IsAccepted);

            if (!isFriend)
            {
                return Json(new { success = false, message = "Bạn chỉ có thể gửi tin nhắn cho bạn bè" });
            }

            var message = new Message
            {
                SenderId = currentUserId.Value,
                ReceiverId = receiverId,
                Content = content.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Broadcast message details via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveDirectMessage", new
            {
                messageId = message.MessageId,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                createdAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });

            return Json(new { success = true });
        }
    }
}
