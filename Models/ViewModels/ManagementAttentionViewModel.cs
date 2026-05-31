using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class ManagementAttentionViewModel : BaseIssueViewModel
    {
        public ManagementAttentionViewModel()
        {
            Type = IssueType.ManagementAttention;
        }
        [Display(Name = "Issue Category")]
        public string ManagementCategory { get; set; } // Infrastructure, Policy, Facilities, Services

        [Display(Name = "Affected Area")]
        public string AffectedArea { get; set; } // Classroom, Library, Canteen, Grounds, Administration

        [Display(Name = "Severity Level")]
        public string SeverityLevel { get; set; } // Low, Medium, High, Critical

        [Display(Name = "Has this been reported before?")]
        public bool IsRepeatedIssue { get; set; }

        [Display(Name = "Previous complaint reference")]
        public string PreviousReference { get; set; }

        [Display(Name = "Suggestions for resolution")]
        public string Suggestions { get; set; }

        [Display(Name = "Expected resolution timeframe")]
        public string ExpectedTimeframe { get; set; }

        [Display(Name = "Affects many students?")]
        public bool AffectsManyStudents { get; set; }

        [Display(Name = "Number of affected students")]
        public int? AffectedStudentCount { get; set; }

        [Display(Name = "Photo/Video evidence")]
        public string EvidenceUrl { get; set; }

        [Display(Name = "Immediate attention needed?")]
        public bool ImmediateAttention { get; set; }

    }
}
