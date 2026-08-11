using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;
using Social_Website.Helpers;

namespace Social_Website.Controllers
{
    public class ProfileController : Controller
    {
        private readonly SocialDbContext _context;

        public ProfileController(SocialDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (this.IsAdmin())
            {
                return RedirectToAction("Index", "Admin");
            }

            var user = await _context.Users.FindAsync(currentUserId.Value);
            if (user == null)
            {
                HttpContext.Session.Clear();
                return RedirectToAction("Login", "Auth");
            }

            var totalPosts = await _context.Posts.CountAsync(p => p.UserId == user.UserId);
            var totalComments = await _context.Comments.CountAsync(c => c.UserId == user.UserId);
            var totalFriends = await _context.Friendships.CountAsync(f => 
                (f.RequestorId == user.UserId || f.ReceiverId == user.UserId) && f.IsAccepted);

            var viewModel = new ProfileViewModel
            {
                User = user,
                TotalPosts = totalPosts,
                TotalComments = totalComments,
                TotalFriends = totalFriends
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateProfile(string fullName, IFormFile? avatarFile)
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrEmpty(fullName) || string.IsNullOrWhiteSpace(fullName))
            {
                return Json(new { success = false, message = "Họ tên không được để trống" });
            }

            var user = await _context.Users.FindAsync(currentUserId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            user.FullName = fullName.Trim();
            HttpContext.Session.SetString("FullName", user.FullName);

            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Validate file extension
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".svg" };
                var extension = Path.GetExtension(avatarFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    return Json(new { success = false, message = "Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif, .svg" });
                }

                // Check directory
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Create unique file name
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                user.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
                HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl);
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "Cập nhật thông tin thành công!",
                fullName = user.FullName,
                avatarUrl = user.AvatarUrl
            });
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            var currentUserId = this.GetCurrentUserId();
            if (currentUserId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                return Json(new { success = false, message = "Vui lòng điền đầy đủ các thông tin mật khẩu" });
            }

            if (newPassword.Length < 6)
            {
                return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 ký tự" });
            }

            if (newPassword != confirmPassword)
            {
                return Json(new { success = false, message = "Xác nhận mật khẩu mới không khớp" });
            }

            var user = await _context.Users.FindAsync(currentUserId.Value);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            // Verify current password
            var currentHash = SeedData.HashPassword(currentPassword);
            if (user.PasswordHash != currentHash)
            {
                return Json(new { success = false, message = "Mật khẩu hiện tại không chính xác" });
            }

            // Hash and update new password
            user.PasswordHash = SeedData.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thay đổi mật khẩu thành công!" });
        }
    }
}
