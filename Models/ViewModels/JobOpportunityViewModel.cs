using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class JobOpportunityViewModel : BaseIssueViewModel
    {
        public JobOpportunityViewModel()
        {
            Type = IssueType.JobOpportunity;
        }

        [Display(Name = "Graduation Year")]
        [Range(2026, 2030, ErrorMessage = "Enter valid graduation year")]
        public int GraduationYear { get; set; }

        [Display(Name = "CGPA")]
        [Range(0, 4, ErrorMessage = "CGPA must be between 0 and 4")]
        public decimal CGPA { get; set; }

        [Display(Name = "Job Type Looking For")]
        public string JobType { get; set; } // Full-time, Part-time, Remote, Onsite

        [Display(Name = "Preferred Job Role")]
        public string PreferredRole { get; set; }

        [Display(Name = "Expected Salary Range")]
        public string ExpectedSalary { get; set; }

        [Display(Name = "Ready for interview?")]
        public bool ReadyForInterview { get; set; }

        [Display(Name = "Need placement assistance?")]
        public bool NeedPlacementAssistance { get; set; }

        [Display(Name = "LinkedIn Profile")]
        [Url(ErrorMessage = "Invalid URL")]
        public string LinkedInProfile { get; set; }

        [Display(Name = "GitHub/Portfolio Link")]
        [Url(ErrorMessage = "Invalid URL")]
        public string PortfolioLink { get; set; }

        [Display(Name = "Certifications")]
        public string Certifications { get; set; }

        [Display(Name = "Interested in")]
        public string PreferredIndustry { get; set; }
    }
}
