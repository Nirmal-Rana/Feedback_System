using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models
{
    // One row = one teacher teaching one subject, in one semester, to one or more sections.
    // A teacher with multiple subjects/semesters simply has multiple rows.
    public class TeacherAssignment
    {
        public int Id { get; set; }

        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }

        [Required]
        [StringLength(100)]
        public string Semester { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        // Comma-separated sections for this semester+subject, e.g. "A, B, C"
        [StringLength(100)]
        public string? Section { get; set; }
    }
}