using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CollegeIssueManagement.Models
{
    public enum IssueType
    {
        Internet, IDCard, Internship, JobOpportunity,
        FacultyFeedback, Counselling, ManagementAttention, Others
    }

    public enum IssueStatus
    {
        Pending, Approved, Rejected, Resolved
    }

    public class Issue
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string StudentName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string StudentEmail { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string StudentRollNo { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string StudentDepartment { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(20)]
        public string StudentPhone { get; set; } = string.Empty;

        [Required]
        public IssueType Type { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public IssueStatus Status { get; set; } = IssueStatus.Pending;

        public DateTime SubmittedDate { get; set; } = DateTime.Now;

        public DateTime? ResolvedDate { get; set; }

        [StringLength(500)]
        public string? AdminRemarks { get; set; }

        public string? QRCodeData { get; set; }

        public bool IsNotified { get; set; } = false;

        [Column(TypeName = "nvarchar(max)")]
        public string? AdditionalData { get; set; }

        [StringLength(450)]
        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}