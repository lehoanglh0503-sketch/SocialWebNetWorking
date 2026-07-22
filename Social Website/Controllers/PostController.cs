using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Social_Website.Hubs;
using Social_Website.Models;

namespace Social_Website.Controllers
{
    public class PostController : Controller
    {
        private readonly SocialDbContext _context;
        private readonly IHubContext<SocialHub> _hubContext;

        public PostController(SocialDbContext context, IHubContext<SocialHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        private User? GetCurrentUser()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (userIdStr == null) return null;

            long userId = long.Parse(userIdStr);
            return _context.Users.Find(userId);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string content)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrEmpty(content) || content.Trim().Length == 0)
            {
                return Json(new { success = false, message = "Nội dung bài viết không được rỗng" });
            }

            var post = new Post
            {
                Content = content.Trim(),
                CreatedAt = DateTime.Now,
                UserId = currentUser.UserId
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Broadcast bài viết mới qua SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveNewPost", new
            {
                postId = post.PostId,
                content = post.Content,
                createdAt = post.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                fullName = currentUser.FullName,
                avatarUrl = currentUser.AvatarUrl,
                username = currentUser.Username
            });

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(long postId, string content)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrEmpty(content) || content.Trim().Length == 0)
            {
                return Json(new { success = false, message = "Nội dung bình luận không được rỗng" });
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài viết" });
            }

            var comment = new Comment
            {
                Content = content.Trim(),
                CreatedAt = DateTime.Now,
                PostId = postId,
                UserId = currentUser.UserId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Broadcast bình luận mới qua SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveNewComment", new
            {
                postId = comment.PostId,
                commentId = comment.CommentId,
                content = comment.Content,
                createdAt = comment.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                fullName = currentUser.FullName,
                avatarUrl = currentUser.AvatarUrl,
                username = currentUser.Username
            });

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(long postId)
        {
            var currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var post = await _context.Posts.Include(p => p.Likes).FirstOrDefaultAsync(p => p.PostId == postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bài viết" });
            }

            var existingLike = post.Likes.FirstOrDefault(l => l.UserId == currentUser.UserId);
            bool isLiked;

            if (existingLike != null)
            {
                _context.PostLikes.Remove(existingLike);
                isLiked = false;
            }
            else
            {
                var like = new PostLike
                {
                    PostId = postId,
                    UserId = currentUser.UserId
                };
                _context.PostLikes.Add(like);
                isLiked = true;
            }

            await _context.SaveChangesAsync();

            int likeCount = await _context.PostLikes.CountAsync(l => l.PostId == postId);

            // Broadcast cập nhật lượt thích qua SignalR
            await _hubContext.Clients.All.SendAsync("UpdateLikeCount", postId, likeCount);

            return Json(new { success = true, isLiked = isLiked, likeCount = likeCount });
        }
    }
}
