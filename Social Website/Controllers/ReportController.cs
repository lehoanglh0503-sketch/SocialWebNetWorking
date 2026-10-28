using Microsoft.AspNetCore.Mvc;
using Social_Website.Models;
using Social_Website.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Social_Website.Controllers
{
    public class ReportController : Controller
    {
        private readonly SocialDbContext _context;

        public ReportController(SocialDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Create(long postId, string reason)
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để báo cáo bài viết." });
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                return Json(new { success = false, message = "Lý do báo cáo không được để trống." });
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại hoặc đã bị xóa." });
            }

            long reporterId = currentUserId.Value;

            // Không cho phép tự báo cáo bài viết của mình
            if (post.UserId == reporterId)
            {
                return Json(new { success = false, message = "Bạn không thể tự báo cáo bài viết của chính mình." });
            }

            // Kiểm tra xem đã báo cáo bài viết này chưa
            var existingReport = await _context.PostReports
                .FirstOrDefaultAsync(r => r.PostId == postId && r.ReporterId == reporterId);
            if (existingReport != null)
            {
                return Json(new { success = false, message = "Bạn đã gửi báo cáo cho bài viết này rồi. Quản trị viên đang xem xét!" });
            }

            var report = new PostReport
            {
                PostId = postId,
                ReporterId = reporterId,
                Reason = reason.Trim(),
                CreatedAt = DateTime.Now
            };

            _context.PostReports.Add(report);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Gửi báo cáo vi phạm thành công! Cảm ơn bạn đã hỗ trợ xây dựng cộng đồng an toàn." });
        }
    }
}
