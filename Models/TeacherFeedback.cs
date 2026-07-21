using System.ComponentModel.DataAnnotations;
namespace CollegeIssueManagement.Models
{
    public class TeacherFeedback
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }
        [Required]
        public string StudentName { get; set; } = string.Empty;
        [Required]
        public string LCID { get; set; } = string.Empty;
        [Required]
        public string Semester { get; set; } = string.Empty;
        [Required]
        public string Subject { get; set; } = string.Empty;

        // NEW — the specific section the student belongs to, so admin can
        // filter feedback by section on the Teacher Detail page.
        [StringLength(50)]
        public string? Section { get; set; }

        [Required]
        public string Rating { get; set; } = string.Empty;
        [Required]
        [StringLength(2000)]
        public string FeedbackText { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; } = false;
        public DateTime SubmittedDate { get; set; } = DateTime.Now;
        public bool IsReviewed { get; set; } = false;
    }
}