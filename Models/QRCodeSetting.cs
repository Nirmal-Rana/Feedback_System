using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models
{
    public class QRCodeSetting
    {
        public int Id { get; set; }

        [Required]
        public string IssueType { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public bool IsEnabled { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}