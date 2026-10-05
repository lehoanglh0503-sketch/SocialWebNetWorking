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
        private readonly IConfiguration _configuration;

        public PostController(SocialDbContext context, IHubContext<SocialHub> hubContext, IConfiguration configuration)
        {
            _context = context;
            _hubContext = hubContext;
            _configuration = configuration;
        }

        private static bool IsValidImageFile(IFormFile file)
        {
            var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext)) return false;

            try
            {
                using var stream = file.OpenReadStream();
                if (stream.Length < 12) return false;
                byte[] header = new byte[12];
                stream.Read(header, 0, 12);

                if (header[0] == 0x4D && header[1] == 0x5A) return false; // MZ executable
                if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true; // JPEG
                if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true; // PNG
                if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46) return true; // GIF
                if (header[0] == (byte)'R' && header[1] == (byte)'I' && header[2] == (byte)'F' && header[3] == (byte)'F' &&
                    header[8] == (byte)'W' && header[9] == (byte)'E' && header[10] == (byte)'B' && header[11] == (byte)'P') return true; // WEBP

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsValidVideoFile(IFormFile file)
        {
            var allowedExts = new[] { ".mp4", ".webm" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExts.Contains(ext)) return false;

            try
            {
                using var stream = file.OpenReadStream();
                if (stream.Length < 12) return false;
                byte[] header = new byte[12];
                stream.Read(header, 0, 12);

                if (header[0] == 0x4D && header[1] == 0x5A) return false; // MZ executable

                if (ext == ".mp4")
                {
                    return header[4] == (byte)'f' && header[5] == (byte)'t' && header[6] == (byte)'y' && header[7] == (byte)'p';
                }

                if (ext == ".webm")
                {
                    return header[0] == 0x1A && header[1] == 0x45 && header[2] == 0xDF && header[3] == 0xA3;
                }

                return false;
            }
            catch
            {
                return false;
            }
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
        public async Task<IActionResult> Create(string? content, List<IFormFile>? images, IFormFile? image, IFormFile? video)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var selectedImages = new List<IFormFile>();
            if (images != null && images.Count > 0)
            {
                selectedImages.AddRange(images.Where(f => f.Length > 0));
            }
            else if (image != null && image.Length > 0)
            {
                selectedImages.Add(image);
            }

            if (selectedImages.Count > 5)
            {
                selectedImages = selectedImages.Take(5).ToList();
            }

            bool hasContent = !string.IsNullOrWhiteSpace(content);
            bool hasImages = selectedImages.Count > 0;
            bool hasVideo = video != null && video.Length > 0;

            if (!hasContent && !hasImages && !hasVideo)
            {
                return Json(new { success = false, message = "Vui lòng nhập nội dung, chọn ảnh hoặc chọn video để đăng bài" });
            }

            // Validate image files
            foreach (var imgFile in selectedImages)
            {
                if (!IsValidImageFile(imgFile))
                {
                    return Json(new { success = false, message = $"File '{imgFile.FileName}' không phải là ảnh hợp lệ (JPG, PNG, WEBP, GIF)" });
                }
            }

            // Validate video file
            string? uploadedVideoUrl = null;
            if (hasVideo)
            {
                if (video!.Length > 25 * 1024 * 1024)
                {
                    return Json(new { success = false, message = $"Video '{video.FileName}' vượt quá dung lượng tối đa 25MB" });
                }

                if (!IsValidVideoFile(video))
                {
                    return Json(new { success = false, message = $"File '{video.FileName}' không phải là video hợp lệ (MP4, WebM)" });
                }
            }

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

            var uploadedImageUrls = new List<string>();
            if (hasImages)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "posts");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                foreach (var imgFile in selectedImages)
                {
                    var ext = Path.GetExtension(imgFile.FileName);
                    var uniqueFileName = Guid.NewGuid().ToString() + ext;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imgFile.CopyToAsync(fileStream);
                    }
                    uploadedImageUrls.Add("/uploads/posts/" + uniqueFileName);
                }
            }

            if (hasVideo)
            {
                var videoFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "videos");
                if (!Directory.Exists(videoFolder))
                {
                    Directory.CreateDirectory(videoFolder);
                }

                var ext = Path.GetExtension(video!.FileName);
                var uniqueFileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(videoFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await video.CopyToAsync(fileStream);
                }
                uploadedVideoUrl = "/uploads/videos/" + uniqueFileName;
            }

            string? imageUrl = uploadedImageUrls.Count > 0 ? string.Join(",", uploadedImageUrls) : null;

            var post = new Post
            {
                Content = hasContent ? content!.Trim() : string.Empty,
                ImageUrl = imageUrl,
                VideoUrl = uploadedVideoUrl,
                CreatedAt = DateTime.UtcNow,
                UserId = currentUser.UserId
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            if (uploadedImageUrls.Count > 0)
            {
                for (int i = 0; i < uploadedImageUrls.Count; i++)
                {
                    _context.PostImages.Add(new PostImage
                    {
                        PostId = post.PostId,
                        ImageUrl = uploadedImageUrls[i],
                        OrderIndex = i
                    });
                }
                await _context.SaveChangesAsync();
            }

            // Xử lý thông báo Tag nếu có trong bài viết
            await ProcessTagsAndNotifyAsync(post.Content, currentUser.UserId, true, post.PostId);

            // Broadcast bài viết mới qua SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveNewPost", new
            {
                postId = post.PostId,
                content = post.Content,
                imageUrl = post.ImageUrl,
                images = uploadedImageUrls,
                videoUrl = post.VideoUrl,
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

        private static void DeletePhysicalFile(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            try
            {
                var relativePath = relativeUrl.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch { }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var post = await _context.Posts.Include(p => p.Images).FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại hoặc đã bị xóa" });
            }

            // Quyền sở hữu: Tác giả bài viết hoặc Admin
            if (post.UserId != currentUser.UserId && !currentUser.IsAdmin)
            {
                return Json(new { success = false, message = "Bạn không có quyền xóa bài viết này" });
            }

            if (post.Images != null && post.Images.Count > 0)
            {
                foreach (var img in post.Images)
                {
                    DeletePhysicalFile(img.ImageUrl);
                }
            }
            if (!string.IsNullOrEmpty(post.ImageUrl))
            {
                foreach (var url in post.ImageUrl.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    DeletePhysicalFile(url);
                }
            }
            if (!string.IsNullOrEmpty(post.VideoUrl))
            {
                DeletePhysicalFile(post.VideoUrl);
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            // Broadcast sự kiện xóa bài viết để các client khác cập nhật UI
            await _hubContext.Clients.All.SendAsync("ReceiveDeletedPost", id, post.UserId);

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

        [HttpPost]
        public async Task<IActionResult> EditPost(long postId, string? content, bool removeImage)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại" });
            }

            // Server-side check ownership
            if (post.UserId != currentUser.UserId)
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền chỉnh sửa bài viết này" });
            }

            bool hasContent = !string.IsNullOrWhiteSpace(content);
            bool hasImage = !removeImage && !string.IsNullOrEmpty(post.ImageUrl);

            if (!hasContent && !hasImage)
            {
                return Json(new { success = false, message = "Bài viết không được để trống cả nội dung và ảnh" });
            }

            if (hasContent)
            {
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

            post.Content = hasContent ? content!.Trim() : string.Empty;
            if (removeImage)
            {
                post.ImageUrl = null;
            }
            post.UpdatedAt = DateTime.Now;

            _context.Posts.Update(post);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Cập nhật bài viết thành công!",
                content = post.Content,
                imageUrl = post.ImageUrl,
                updatedAt = post.UpdatedAt?.ToVietnamTimeString()
            });
        }

        [HttpPost]
        public async Task<IActionResult> ToggleSavePost(long postId)
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại" });
            }

            var saved = await _context.SavedPosts.FirstOrDefaultAsync(sp => sp.UserId == currentUser.UserId && sp.PostId == postId);
            if (saved != null)
            {
                _context.SavedPosts.Remove(saved);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isSaved = false, message = "Đã bỏ lưu bài viết" });
            }
            else
            {
                var newSaved = new SavedPost
                {
                    UserId = currentUser.UserId,
                    PostId = postId,
                    SavedAt = DateTime.Now
                };
                _context.SavedPosts.Add(newSaved);
                await _context.SaveChangesAsync();
                return Json(new { success = true, isSaved = true, message = "Đã lưu bài viết" });
            }
        }

        [HttpGet("Saved")]
        [HttpGet("Post/Saved")]
        public async Task<IActionResult> Saved()
        {
            var currentUser = this.GetCurrentUser(_context);
            if (currentUser == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            var savedPostIds = await _context.SavedPosts
                .Where(sp => sp.UserId == currentUser.UserId)
                .OrderByDescending(sp => sp.SavedAt)
                .Select(sp => sp.PostId)
                .ToListAsync();

            var posts = await _context.Posts
                .Include(p => p.User)
                .Include(p => p.Images)
                .Include(p => p.Likes)
                .Include(p => p.Comments).ThenInclude(c => c.User)
                .Where(p => savedPostIds.Contains(p.PostId))
                .ToListAsync();

            var orderedPosts = savedPostIds
                .Select(id => posts.FirstOrDefault(p => p.PostId == id))
                .Where(p => p != null)
                .ToList()!;

            ViewBag.CurrentUser = currentUser;
            ViewBag.SavedPostIds = savedPostIds;
            return View("Saved", orderedPosts);
        }
    }
}
