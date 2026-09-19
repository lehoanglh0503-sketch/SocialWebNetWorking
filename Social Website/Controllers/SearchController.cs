using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;
using Social_Website.Helpers;

namespace Social_Website.Controllers
{
    public class SearchController : Controller
    {
        private readonly SocialDbContext _context;

        public SearchController(SocialDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q)
        {
            var currentUser = this.GetCurrentUser(_context);
            ViewBag.CurrentUser = currentUser;

            var model = new SearchViewModel
            {
                Query = q?.Trim() ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(model.Query))
            {
                return View(model);
            }

            long currentUserId = currentUser?.UserId ?? 0;
            string keyword = model.Query.ToLower();

            // 1. Tìm kiếm Người dùng
            var matchingUsers = await _context.Users
                .Where(u => !u.IsAdmin && u.UserId != currentUserId &&
                            (u.FullName.ToLower().Contains(keyword) || u.Username.ToLower().Contains(keyword)))
                .Take(20)
                .ToListAsync();

            if (currentUserId > 0)
            {
                var friendships = await _context.Friendships
                    .Where(f => f.RequestorId == currentUserId || f.ReceiverId == currentUserId)
                    .ToListAsync();

                foreach (var user in matchingUsers)
                {
                    var friendship = friendships.FirstOrDefault(f =>
                        (f.RequestorId == currentUserId && f.ReceiverId == user.UserId) ||
                        (f.RequestorId == user.UserId && f.ReceiverId == currentUserId));

                    string status = "None";
                    if (friendship != null)
                    {
                        if (friendship.IsAccepted)
                        {
                            status = "Friends";
                        }
                        else if (friendship.RequestorId == currentUserId)
                        {
                            status = "PendingSent";
                        }
                        else
                        {
                            status = "PendingReceived";
                        }
                    }

                    model.Users.Add(new SearchUserDto
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Username = user.Username,
                        AvatarUrl = user.AvatarUrl,
                        FriendshipStatus = status
                    });
                }
            }
            else
            {
                foreach (var user in matchingUsers)
                {
                    model.Users.Add(new SearchUserDto
                    {
                        UserId = user.UserId,
                        FullName = user.FullName,
                        Username = user.Username,
                        AvatarUrl = user.AvatarUrl,
                        FriendshipStatus = "None"
                    });
                }
            }

            // 2. Tìm kiếm Bài viết
            model.Posts = await _context.Posts
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
                .Where(p => (p.Content != null && p.Content.ToLower().Contains(keyword)) ||
                            (p.User != null && (p.User.FullName.ToLower().Contains(keyword) || p.User.Username.ToLower().Contains(keyword))))
                .OrderByDescending(p => p.CreatedAt)
                .Take(20)
                .ToListAsync();

            return View(model);
        }
    }
}
