using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Social_Website.Models;
using Social_Website.Services;
using Social_Website.Helpers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Social_Website.Controllers
{
    public class AuthController : Controller
    {
        private readonly SocialDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;

        public AuthController(SocialDbContext context, IEmailSender emailSender, IConfiguration configuration)
        {
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (this.GetCurrentUserId() != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ tên tài khoản và mật khẩu");
                return View();
            }

            string passwordHash = SeedData.HashPassword(password);
            var user = _context.Users.FirstOrDefault(u => u.Username == username && u.PasswordHash == passwordHash);

            if (user == null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu không chính xác");
                return View();
            }

            if (user.IsLocked)
            {
                if (user.LockReason == "Manual" || (string.IsNullOrEmpty(user.LockReason) && user.ApprovedReportCount < 3))
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị quản trị viên khóa.");
                }
                else
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị KHÓA do vi phạm tiêu chuẩn cộng đồng (đã có 3 lần báo cáo vi phạm được Admin phê duyệt). Vui lòng liên hệ Quản trị viên.");
                }
                return View();
            }

            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            if (user.ApprovedReportCount == 2)
            {
                TempData["AccountWarning"] = "CẢNH BÁO VI PHẠM: Tài khoản của bạn đã bị báo cáo và được Admin phê duyệt 2 lần. Nếu vi phạm thêm 1 lần nữa (tổng 3 lần), tài khoản của bạn sẽ bị KHÓA vĩnh viễn!";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (this.GetCurrentUserId() != null)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string fullName, string email, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ các thông tin bắt buộc");
                return View();
            }

            if (username.Length < 3 || username.Length > 50)
            {
                ModelState.AddModelError("", "Tên tài khoản phải từ 3 đến 50 ký tự");
                return View();
            }

            var emailNormalized = email.Trim().ToLower();
            if (!Regex.IsMatch(emailNormalized, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError("", "Địa chỉ email không đúng định dạng");
                return View();
            }

            if (password.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu phải chứa ít nhất 6 ký tự");
                return View();
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu không khớp");
                return View();
            }

            if (_context.Users.Any(u => u.Username.ToLower() == username.Trim().ToLower()))
            {
                ModelState.AddModelError("", "Tên tài khoản đã tồn tại");
                return View();
            }

            if (_context.Users.Any(u => u.Email != null && u.Email.ToLower() == emailNormalized))
            {
                ModelState.AddModelError("", "Email đã được sử dụng");
                return View();
            }

            var user = new User
            {
                Username = username.Trim().ToLower(),
                FullName = fullName.Trim(),
                Email = emailNormalized,
                PasswordHash = SeedData.HashPassword(password),
                AvatarUrl = $"https://api.dicebear.com/7.x/adventurer/svg?seed={Uri.EscapeDataString(username)}"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            string neutralMessage = "Nếu email tồn tại trong hệ thống, chúng tôi đã gửi hướng dẫn đặt lại mật khẩu.";

            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Message = neutralMessage;
                return View();
            }

            string emailNormalized = email.Trim().ToLower();
            DateTime oneHourAgo = DateTime.Now.AddHours(-1);

            // Rate limit check: max 3 requests per hour for email
            var recentRequestsCount = await _context.PasswordResetTokens
                .CountAsync(t => (t.User != null && t.User.Email != null && t.User.Email.ToLower() == emailNormalized)
                              && t.CreatedAt >= oneHourAgo);

            if (recentRequestsCount >= 3)
            {
                ViewBag.Message = neutralMessage;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == emailNormalized);
            if (user != null)
            {
                // Invalidate old unused tokens for user
                var oldTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.UserId && t.UsedAt == null)
                    .ToListAsync();
                foreach (var oldToken in oldTokens)
                {
                    oldToken.UsedAt = DateTime.Now;
                }

                // Generate 32 bytes cryptographically random token
                byte[] tokenBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(tokenBytes);
                }
                string rawToken = Convert.ToHexString(tokenBytes);
                string tokenHash = SeedData.HashPassword(rawToken);

                var resetToken = new PasswordResetToken
                {
                    UserId = user.UserId,
                    TokenHash = tokenHash,
                    ExpiresAt = DateTime.Now.AddMinutes(30),
                    CreatedAt = DateTime.Now
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                var baseUrl = _configuration["AppConfig:BaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
                var resetLink = $"{baseUrl}/Auth/ResetPassword?token={Uri.EscapeDataString(rawToken)}&email={Uri.EscapeDataString(user.Email!)}";

                string emailBody = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 10px;'>
                        <h2 style='color: #4f46e5;'>Connectify - Đặt lại mật khẩu</h2>
                        <p>Xin chào <strong>{user.FullName}</strong>,</p>
                        <p>Chúng tôi đã nhận được yêu cầu đặt lại mật khẩu cho tài khoản Connectify của bạn.</p>
                        <p style='margin: 25px 0;'>
                            <a href='{resetLink}' style='background-color: #4f46e5; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 8px; font-weight: bold; display: inline-block;'>
                                Đặt lại mật khẩu ngay
                            </a>
                        </p>
                        <p style='color: #64748b; font-size: 0.9em;'>Liên kết này có hiệu lực trong 30 phút và chỉ sử dụng được 1 lần.</p>
                        <hr style='border: none; border-top: 1px solid #e2e8f0; margin: 20px 0;' />
                        <p style='color: #94a3b8; font-size: 0.8em;'>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này. Mật khẩu của bạn vẫn an toàn.</p>
                    </div>";

                _ = Task.Run(() => _emailSender.SendEmailAsync(user.Email!, "Connectify - Đặt lại mật khẩu", emailBody));
            }

            ViewBag.Message = neutralMessage;
            return View();
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Liên kết đặt lại mật khẩu không hợp lệ.");
            }
            ViewBag.Token = token;
            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string token, string email, string newPassword, string confirmPassword)
        {
            ViewBag.Token = token;
            ViewBag.Email = email;

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ các thông tin bắt buộc.");
                return View();
            }

            if (newPassword.Length < 6)
            {
                ModelState.AddModelError("", "Mật khẩu mới phải chứa ít nhất 6 ký tự.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Xác nhận mật khẩu không khớp.");
                return View();
            }

            string emailNormalized = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email != null && u.Email.ToLower() == emailNormalized);
            if (user == null)
            {
                ModelState.AddModelError("", "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.");
                return View();
            }

            string tokenHash = SeedData.HashPassword(token);
            var resetToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.UserId == user.UserId
                                       && t.TokenHash == tokenHash
                                       && t.UsedAt == null
                                       && t.ExpiresAt > DateTime.Now);

            if (resetToken == null)
            {
                ModelState.AddModelError("", "Liên kết đặt lại mật khẩu không hợp lệ, đã hết hạn hoặc đã được sử dụng.");
                return View();
            }

            // Update user password & mark token as used
            user.PasswordHash = SeedData.HashPassword(newPassword);
            resetToken.UsedAt = DateTime.Now;

            _context.Users.Update(user);
            _context.PasswordResetTokens.Update(resetToken);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.";
            return RedirectToAction("Login");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
