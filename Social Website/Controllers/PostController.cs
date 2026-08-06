using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Social_Website.Hubs;
using Social_Website.Models;
using Social_Website.Helpers;

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

        private async Task<(bool contains, List<string> detectedWords)> ContainsViolentWordsAsync(string text)
        {
            var detectedWords = new List<string>();
            if (string.IsNullOrEmpty(text)) return (false, detectedWords);

            string lowerText = text.ToLower();

            // Lấy toàn bộ từ cấm từ database
            var bannedWords = await _context.BannedWords.Select(b => b.Word).ToListAsync();

            foreach (var word in bannedWords)
            {
                string pattern = @"(?i)(?:\s|^)" + System.Text.RegularExpressions.Regex.Escape(word.ToLower()) + @"(?:\s|[.,!?;:]|$)";
                if (System.Text.RegularExpressions.Regex.IsMatch(lowerText, pattern))
                {
                    if (!detectedWords.Contains(word))
                    {
                        detectedWords.Add(word);
                    }
                }
            }

            return (detectedWords.Count > 0, detectedWords);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string? content, IFormFile? image)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            bool hasContent = !string.IsNullOrWhiteSpace(content);
            bool hasImage = image != null && image.Length > 0;

            if (!hasContent && !hasImage)
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung hoặc chọn ảnh để đăng bài" });
            }

            string? imageUrl = null;

            if (hasContent)
            {
                // Kiểm tra từ ngữ bạo lực từ Database
                var (containsViolent, detected) = await ContainsViolentWordsAsync(content!);
                if (containsViolent)
                {
                    return Json(new
                    {
                        success = false,
                        isViolent = true,
                        message = $"Bài viết của bạn chứa từ ngữ không phù hợp: \"{string.Join("\", \"", detected)}\". Vui lòng sửa lại."
                    });
                }
            }

            if (hasImage)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
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

                imageUrl = "/uploads/posts/" + uniqueFileName;
            }

            var post = new Post
            {
                Content = hasContent ? content!.Trim() : string.Empty,
                ImageUrl = imageUrl,
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
                imageUrl = post.ImageUrl,
                createdAt = post.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                fullName = currentUser.FullName,
                avatarUrl = currentUser.AvatarUrl,
                username = currentUser.Username,
                userId = currentUser.UserId
            });

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(long postId, string content)
        {
            var currentUser = this.GetCurrentUser(_context);
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

            // Kiểm tra từ ngữ bạo lực từ Database cho bình luận
            var (containsViolent, detected) = await ContainsViolentWordsAsync(content);
            if (containsViolent)
            {
                return Json(new
                {
                    success = false,
                    isViolent = true,
                    message = $"Bình luận của bạn chứa từ ngữ không phù hợp: \"{string.Join("\", \"", detected)}\". Vui lòng sửa lại."
                });
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
                username = currentUser.Username,
                userId = currentUser.UserId,
                postAuthorUserId = post.UserId
            });

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(long postId, string reactionType = "Like")
        {
            var currentUser = this.GetCurrentUser(_context);
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
            bool isLiked = false;
            string? currentReaction = null;

            if (existingLike != null)
            {
                if (existingLike.ReactionType == reactionType)
                {
                    _context.PostLikes.Remove(existingLike);
                    isLiked = false;
                }
                else
                {
                    existingLike.ReactionType = reactionType;
                    _context.PostLikes.Update(existingLike);
                    isLiked = true;
                    currentReaction = reactionType;
                }
            }
            else
            {
                var like = new PostLike
                {
                    PostId = postId,
                    UserId = currentUser.UserId,
                    ReactionType = reactionType
                };
                _context.PostLikes.Add(like);
                isLiked = true;
                currentReaction = reactionType;
            }

            await _context.SaveChangesAsync();

            var postLikes = await _context.PostLikes.Where(l => l.PostId == postId).ToListAsync();
            int totalCount = postLikes.Count;
            var topReactions = postLikes
                .GroupBy(l => l.ReactionType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3)
                .ToList();

            // Broadcast cập nhật tương tác qua SignalR
            await _hubContext.Clients.All.SendAsync("UpdatePostReactions", postId, totalCount, topReactions);

            return Json(new
            {
                success = true,
                isLiked = isLiked,
                reactionType = currentReaction,
                likeCount = totalCount,
                topReactions = topReactions
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetPostReactions(long postId)
        {
            var reactions = await _context.PostLikes
                .Where(l => l.PostId == postId)
                .Include(l => l.User)
                .Select(l => new
                {
                    userId = l.UserId,
                    fullName = l.User!.FullName,
                    avatarUrl = l.User.AvatarUrl,
                    username = l.User.Username,
                    reactionType = l.ReactionType
                })
                .ToListAsync();

            return Json(new { success = true, reactions = reactions });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại hoặc đã bị xóa" });
            }

            // Quyền sở hữu: Tác giả bài viết hoặc Admin
            if (post.UserId != currentUser.UserId && !currentUser.IsAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa bài viết này" });
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            // Broadcast sự kiện xóa bài viết để các client khác cập nhật UI
            await _hubContext.Clients.All.SendAsync("ReceiveDeletedPost", id);

            return Json(new { success = true, message = "Đã xóa bài viết thành công!" });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteComment(long id)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var comment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.CommentId == id);
            if (comment == null)
            {
                return Json(new { success = false, message = "Bình luận không tồn tại hoặc đã bị xóa" });
            }

            // Quyền xóa bình luận: Tác giả bình luận, Tác giả bài viết, hoặc Admin
            if (comment.UserId != currentUser.UserId && comment.Post?.UserId != currentUser.UserId && !currentUser.IsAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa bình luận này" });
            }

            long postId = comment.PostId;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            // Broadcast sự kiện xóa bình luận qua SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveDeletedComment", id, postId);

            return Json(new { success = true, message = "Đã xóa bình luận thành công" });
        }
    }
}
