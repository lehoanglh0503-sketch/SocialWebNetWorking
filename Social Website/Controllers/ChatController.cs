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
                .Include(m => m.SharedPost!)
                    .ThenInclude(p => p.User)
                .Where(m => (m.SenderId == currentUserId.Value && m.ReceiverId == friendId) ||
                            (m.SenderId == friendId && m.ReceiverId == currentUserId.Value))
                .OrderBy(m => m.CreatedAt)
                .Select(m => new
                {
                    messageId = m.MessageId,
                    senderId = m.SenderId,
                    receiverId = m.ReceiverId,
                    content = m.Content,
                    imageUrl = m.ImageUrl,
                    sharedPostId = m.SharedPostId,
                    sharedPost = m.SharedPost == null ? null : new
                    {
                        postId = m.SharedPost.PostId,
                        content = m.SharedPost.Content,
                        imageUrl = m.SharedPost.ImageUrl,
                        authorName = m.SharedPost.User != null ? m.SharedPost.User.FullName : "Người dùng",
                        authorAvatar = m.SharedPost.User != null ? m.SharedPost.User.AvatarUrl : ""
                    },
                    createdAt = m.CreatedAt.ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            return Json(new { success = true, messages = messages });
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage(long receiverId, string? content, IFormFile? image, long? sharedPostId)
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            bool hasContent = !string.IsNullOrWhiteSpace(content);
            bool hasImage = image != null && image.Length > 0;
            bool hasSharedPost = sharedPostId.HasValue && sharedPostId.Value > 0;

            if (!hasContent && !hasImage && !hasSharedPost)
            {
                return Json(new { success = false, message = "Nội dung tin nhắn không được để trống" });
            }

            // Verify they are friends
            var isFriend = await _context.Friendships.AnyAsync(f =>
                ((f.RequestorId == currentUserId.Value && f.ReceiverId == receiverId) ||
                 (f.RequestorId == receiverId && f.ReceiverId == currentUserId.Value)) && f.IsAccepted);

            if (!isFriend)
            {
                return Json(new { success = false, message = "Bạn chỉ có thể gửi tin nhắn cho bạn bè" });
            }

            string? chatImageUrl = null;
            if (hasImage)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "chat");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(image!.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                chatImageUrl = "/uploads/chat/" + uniqueFileName;
            }

            var message = new Message
            {
                SenderId = currentUserId.Value,
                ReceiverId = receiverId,
                Content = hasContent ? content!.Trim() : string.Empty,
                ImageUrl = chatImageUrl,
                SharedPostId = hasSharedPost ? sharedPostId : null,
                CreatedAt = DateTime.Now
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            object? sharedPostData = null;
            if (message.SharedPostId.HasValue)
            {
                var post = await _context.Posts
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PostId == message.SharedPostId.Value);

                if (post != null)
                {
                    sharedPostData = new
                    {
                        postId = post.PostId,
                        content = post.Content,
                        imageUrl = post.ImageUrl,
                        authorName = post.User != null ? post.User.FullName : "Người dùng",
                        authorAvatar = post.User != null ? post.User.AvatarUrl : ""
                    };
                }
            }

            // Broadcast message details via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveDirectMessage", new
            {
                messageId = message.MessageId,
                senderId = message.SenderId,
                receiverId = message.ReceiverId,
                content = message.Content,
                imageUrl = message.ImageUrl,
                sharedPostId = message.SharedPostId,
                sharedPost = sharedPostData,
                createdAt = message.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            });

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetFriendsForShare()
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var friends = await _context.Friendships
                .Where(f => (f.RequestorId == currentUserId.Value || f.ReceiverId == currentUserId.Value) && f.IsAccepted)
                .Select(f => f.RequestorId == currentUserId.Value ? f.Receiver : f.Requestor)
                .Select(u => new
                {
                    userId = u!.UserId,
                    fullName = u.FullName,
                    username = u.Username,
                    avatarUrl = u.AvatarUrl
                })
                .ToListAsync();

            return Json(new { success = true, friends = friends });
        }
    }
}
