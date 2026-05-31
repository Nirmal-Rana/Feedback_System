using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models
{
    public class Admin
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        public DateTime LastLogin { get; set; } = DateTime.Now;
    }
}