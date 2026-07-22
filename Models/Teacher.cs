using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models
{
    public class Teacher
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Designation { get; set; }

        [StringLength(300)]
        public string? PhotoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // A teacher can teach many subjects across many semesters/sections.
        public List<TeacherAssignment> Assignments { get; set; } = new();
    }
}