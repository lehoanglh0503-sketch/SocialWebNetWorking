using Microsoft.AspNetCore.Mvc;
using Social_Website.Models;
using Social_Website.Helpers;

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
                return Json(new { success = false, message = "Vui lòng đăng nhập để báo cáo bài viết" });
            }

            if (string.IsNullOrEmpty(reason) || reason.Trim().Length == 0)
            {
                return Json(new { success = false, message = "Lý do báo cáo không được để trống" });
            }

            var post = await _context.Posts.FindAsync(postId);
            if (post == null)
            {
                return Json(new { success = false, message = "Bài viết không tồn tại" });
            }

            long reporterId = currentUserId.Value;

            // Kiểm tra xem đã báo cáo bài viết này chưa
            var existingReport = _context.PostReports.FirstOrDefault(r => r.PostId == postId && r.ReporterId == reporterId);
            if (existingReport != null)
            {
                return Json(new { success = false, message = "Bạn đã báo cáo bài viết này rồi" });
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

            return Json(new { success = true, message = "Gửi báo cáo thành công. Cảm ơn sự đóng góp của bạn!" });
        }
    }
}
