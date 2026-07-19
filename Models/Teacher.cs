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

        [StringLength(100)]
        public string? ProfessionalClass { get; set; }

        [StringLength(200)]
        public string? Designation { get; set; }

        // NEW — relative web path, e.g. "/uploads/teachers/3f2a1c.jpg"
        [StringLength(300)]
        public string? PhotoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}