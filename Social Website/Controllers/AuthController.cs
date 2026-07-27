using Microsoft.AspNetCore.Mvc;
using Social_Website.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Social_Website.Helpers;

namespace Social_Website.Controllers
{
    public class AuthController : Controller
    {
        private readonly SocialDbContext _context;

        public AuthController(SocialDbContext context)
        {
            _context = context;
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

            // Lưu thông tin đăng nhập vào Session
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

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
        public IActionResult Register(string username, string fullName, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(fullName) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ các thông tin bắt buộc");
                return View();
            }

            if (username.Length < 3 || username.Length > 50)
            {
                ModelState.AddModelError("", "Tên tài khoản phải từ 3 đến 50 ký tự");
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

            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "Tên tài khoản đã tồn tại");
                return View();
            }

            // Tạo người dùng mới
            var user = new User
            {
                Username = username.Trim().ToLower(),
                FullName = fullName.Trim(),
                PasswordHash = SeedData.HashPassword(password),
                // Tạo avatar ngẫu nhiên theo tên người dùng
                AvatarUrl = $"https://api.dicebear.com/7.x/adventurer/svg?seed={Uri.EscapeDataString(username)}"
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            // Đăng nhập luôn sau khi đăng ký thành công
            HttpContext.Session.SetString("UserId", user.UserId.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("AvatarUrl", user.AvatarUrl);
            HttpContext.Session.SetString("IsAdmin", user.IsAdmin.ToString());

            return RedirectToAction("Index", "Home");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
