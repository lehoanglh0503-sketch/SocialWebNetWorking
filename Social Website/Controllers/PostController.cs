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
            if (string.IsNullOrWhiteSpace(text)) return (false, detectedWords);

            // Lấy toàn bộ từ cấm từ database
            var bannedWords = await _context.BannedWords.Select(b => b.Word).ToListAsync();
            if (bannedWords.Count == 0) return (false, detectedWords);

            string lowerText = text.ToLower();

            foreach (var word in bannedWords)
            {
                if (string.IsNullOrWhiteSpace(word)) continue;
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
                CreatedAt = DateTime.UtcNow,
                UserId = currentUser.UserId
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Xử lý thông báo Tag nếu có trong bài viết
            await ProcessTagsAndNotifyAsync(post.Content, currentUser.UserId, true, post.PostId);

            // Broadcast bài viết mới qua SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveNewPost", new
            {
                postId = post.PostId,
                content = post.Content,
                imageUrl = post.ImageUrl,
                createdAt = post.CreatedAt.ToVietnamTimeString(),
                fullName = currentUser.FullName,
                avatarUrl = currentUser.AvatarUrl,
                username = currentUser.Username,
                userId = currentUser.UserId
            });

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreateComment(long postId, string content, long? parentCommentId = null)
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
                CreatedAt = DateTime.UtcNow,
                PostId = postId,
                UserId = currentUser.UserId,
                ParentCommentId = parentCommentId
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Xử lý gửi thông báo cho bình luận và tag
            var notifiedUserIds = new HashSet<long>();

            // 1. Thông báo cho tác giả bài viết
            if (currentUser.UserId != post.UserId)
            {
                await CreateAndSendNotificationAsync(post.UserId, currentUser.UserId, "CommentPost", $"{currentUser.FullName} đã bình luận bài viết của bạn.", $"/Home/Index#postCard-{postId}");
                notifiedUserIds.Add(post.UserId);
            }

            // 2. Thông báo khi trả lời bình luận khác
            if (parentCommentId != null)
            {
                var parentComment = await _context.Comments.FindAsync(parentCommentId.Value);
                if (parentComment != null && currentUser.UserId != parentComment.UserId && !notifiedUserIds.Contains(parentComment.UserId))
                {
                    await CreateAndSendNotificationAsync(parentComment.UserId, currentUser.UserId, "ReplyComment", $"{currentUser.FullName} đã trả lời bình luận của bạn.", $"/Home/Index#postCard-{postId}");
                    notifiedUserIds.Add(parentComment.UserId);
                }
            }

            // 3. Thông báo cho người được tag (@username) trong bình luận
            var matches = System.Text.RegularExpressions.Regex.Matches(content, @"@([a-zA-Z0-9_\.]+)");
            if (matches.Count > 0)
            {
                var usernames = matches.Select(m => m.Groups[1].Value.ToLower()).Distinct().ToList();
                var taggedUsers = await _context.Users
                    .Where(u => usernames.Contains(u.Username.ToLower()) && u.UserId != currentUser.UserId)
                    .ToListAsync();

                foreach (var taggedUser in taggedUsers)
                {
                    if (!notifiedUserIds.Contains(taggedUser.UserId))
                    {
                        await CreateAndSendNotificationAsync(taggedUser.UserId, currentUser.UserId, "TagComment", $"{currentUser.FullName} đã nhắc đến bạn trong một bình luận.", $"/Home/Index#postCard-{postId}");
                        notifiedUserIds.Add(taggedUser.UserId);
                    }
                }
            }

            // Broadcast bình luận mới qua SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveNewComment", new
            {
                postId = comment.PostId,
                commentId = comment.CommentId,
                parentCommentId = comment.ParentCommentId,
                content = comment.Content,
                createdAt = comment.CreatedAt.ToVietnamTimeString(),
                fullName = currentUser.FullName,
                avatarUrl = currentUser.AvatarUrl,
                username = currentUser.Username,
                userId = currentUser.UserId,
                postAuthorUserId = post.UserId
            });

            return Json(new { success = true, commentId = comment.CommentId, parentCommentId = comment.ParentCommentId });
        }

        private async Task CreateAndSendNotificationAsync(long targetUserId, long actorId, string type, string message, string? targetUrl)
        {
            if (targetUserId == actorId || targetUserId <= 0) return;

            var currentUser = await _context.Users.FindAsync(actorId);
            if (currentUser == null) return;

            var notification = new Notification
            {
                UserId = targetUserId,
                ActorId = actorId,
                Type = type,
                Message = message,
                TargetUrl = targetUrl,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.Group($"User_{targetUserId}").SendAsync("ReceiveNotification", new
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
        }

        private async Task ProcessTagsAndNotifyAsync(string? text, long actorId, bool isPost, long postId)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            var matches = System.Text.RegularExpressions.Regex.Matches(text, @"@([a-zA-Z0-9_\.]+)");
            if (matches.Count == 0) return;

            var currentUser = await _context.Users.FindAsync(actorId);
            if (currentUser == null) return;

            var usernames = matches.Select(m => m.Groups[1].Value.ToLower()).Distinct().ToList();
            var taggedUsers = await _context.Users
                .Where(u => usernames.Contains(u.Username.ToLower()) && u.UserId != actorId)
                .ToListAsync();

            string targetUrl = $"/Home/Index#postCard-{postId}";

            foreach (var taggedUser in taggedUsers)
            {
                await CreateAndSendNotificationAsync(
                    taggedUser.UserId,
                    actorId,
                    isPost ? "TagPost" : "TagComment",
                    $"{currentUser.FullName} đã nhắc đến bạn trong một {(isPost ? "bài viết" : "bình luận")}.",
                    targetUrl
                );
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleCommentLike(long commentId, string reactionType = "Like")
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var comment = await _context.Comments
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bình luận" });
            }

            var existingLike = comment.Likes.FirstOrDefault(l => l.UserId == currentUser.UserId);
            bool isLiked = false;
            string? currentReaction = null;

            if (existingLike != null)
            {
                if (existingLike.ReactionType == reactionType)
                {
                    _context.CommentLikes.Remove(existingLike);
                    isLiked = false;
                }
                else
                {
                    existingLike.ReactionType = reactionType;
                    _context.CommentLikes.Update(existingLike);
                    isLiked = true;
                    currentReaction = reactionType;
                }
            }
            else
            {
                var like = new CommentLike
                {
                    CommentId = commentId,
                    UserId = currentUser.UserId,
                    ReactionType = reactionType
                };
                _context.CommentLikes.Add(like);
                isLiked = true;
                currentReaction = reactionType;
            }

            await _context.SaveChangesAsync();

            if (isLiked && comment.UserId != currentUser.UserId)
            {
                await CreateAndSendNotificationAsync(comment.UserId, currentUser.UserId, "LikeComment", $"{currentUser.FullName} đã thích bình luận của bạn.", $"/Home/Index#postCard-{comment.PostId}");
            }

            var commentLikes = await _context.CommentLikes.Where(l => l.CommentId == commentId).ToListAsync();
            int totalCount = commentLikes.Count;
            var topReactions = commentLikes
                .GroupBy(l => l.ReactionType)
                .OrderByDescending(g => g.Count())
                .Select(g => g.Key)
                .Take(3)
                .ToList();

            // Broadcast cập nhật lượt thích bình luận qua SignalR
            await _hubContext.Clients.All.SendAsync("UpdateCommentLikes", commentId, comment.PostId, totalCount, topReactions);

            return Json(new
            {
                success = true,
                commentId = commentId,
                isLiked = isLiked,
                reactionType = currentReaction,
                likeCount = totalCount,
                topReactions = topReactions
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetCommentReactions(long commentId)
        {
            var reactions = await _context.CommentLikes
                .Where(l => l.CommentId == commentId)
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

            if (isLiked && post.UserId != currentUser.UserId)
            {
                await CreateAndSendNotificationAsync(post.UserId, currentUser.UserId, "LikePost", $"{currentUser.FullName} đã thích bài viết của bạn.", $"/Home/Index#postCard-{postId}");
            }

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
