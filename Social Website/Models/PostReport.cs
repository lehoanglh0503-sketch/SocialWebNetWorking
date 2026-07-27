using System.ComponentModel.DataAnnotations;

namespace Social_Website.Models
{
    public class PostReport
    {
        public long PostReportId { get; set; }
        
        public long PostId { get; set; }
        public Post? Post { get; set; }
        
        public long ReporterId { get; set; }
        public User? Reporter { get; set; }
        
        [Required(ErrorMessage = "Lý do báo cáo không được để trống")]
        [StringLength(500, ErrorMessage = "Lý do báo cáo không quá 500 ký tự")]
        public string Reason { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
