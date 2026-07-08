using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models
{
    public class AbsenceRecord
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(200)]
        public string StudentName { get; set; } = string.Empty;

        [Required(ErrorMessage = "LCID is required")]
        [StringLength(50)]
        public string LCID { get; set; } = string.Empty;

        [Required(ErrorMessage = "Semester is required")]
        public string Semester { get; set; } = string.Empty;

        [Required(ErrorMessage = "Section is required")]
        public string Section { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please specify which class you missed")]
        [StringLength(200)]
        public string MissedClass { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please provide a reason")]
        [StringLength(1000)]
        public string Reason { get; set; } = string.Empty;

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        public bool IsReviewed { get; set; } = false;

        public DateTime? ReviewedDate { get; set; }
    }
}