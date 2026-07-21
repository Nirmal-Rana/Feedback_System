using System.ComponentModel.DataAnnotations;
namespace CollegeIssueManagement.Models
{
    public class Teacher
    {
        public int Id { get; set; }
        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        public string Semester { get; set; } = string.Empty;

        // RENAMED from ProfessionalClass — the section(s) this teacher is assigned to, e.g. "A, D, F"
        [StringLength(100)]
        public string? Section { get; set; }

        [StringLength(200)]
        public string? Designation { get; set; }
        [StringLength(300)]
        public string? PhotoPath { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}