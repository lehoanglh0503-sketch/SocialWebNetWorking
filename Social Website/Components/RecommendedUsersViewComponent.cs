using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;
using Social_Website.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Social_Website.Components
{
    public class RecommendedUsersViewComponent : ViewComponent
    {
        private readonly SocialDbContext _context;

        public RecommendedUsersViewComponent(SocialDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var loggedInUserId = HttpContext.Session.GetCurrentUserId();
            if (loggedInUserId == null)
            {
                return View(new List<User>());
            }

            long currentUserId = loggedInUserId.Value;

            // Lấy ID của những người đã gửi/nhận kết bạn với người dùng hiện tại
            var relatedUserIds = await _context.Friendships
                .Where(f => f.RequestorId == currentUserId || f.ReceiverId == currentUserId)
                .Select(f => f.RequestorId == currentUserId ? f.ReceiverId : f.RequestorId)
                .ToListAsync();

            // Thêm chính mình vào danh sách loại trừ
            relatedUserIds.Add(currentUserId);

            // Đề xuất kết bạn: Những tài khoản chưa có bất kỳ quan hệ kết bạn nào và không phải là Admin
            var recommendedUsers = await _context.Users
                .Where(u => !relatedUserIds.Contains(u.UserId) && !u.IsAdmin)
                .Take(5)
                .ToListAsync();
            return View(recommendedUsers);
        }
    }
}
