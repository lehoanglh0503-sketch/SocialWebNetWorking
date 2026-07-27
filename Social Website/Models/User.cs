using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class User
    {
        public long UserId { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập tên tài khoản")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên tài khoản phải từ 3 đến 50 ký tự")]
        public string Username { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string PasswordHash { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Vui lòng nhập tên đầy đủ")]
        [StringLength(100, ErrorMessage = "Tên đầy đủ không được vượt quá 100 ký tự")]
        public string FullName { get; set; } = string.Empty;
        
        public string AvatarUrl { get; set; } = string.Empty;
        
        public bool IsAdmin { get; set; } = false;
        
        public ICollection<Post> Posts { get; set; } = new List<Post>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
