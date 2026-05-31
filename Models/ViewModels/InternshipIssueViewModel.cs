using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class InternshipIssueViewModel :BaseIssueViewModel
    {
        public InternshipIssueViewModel()
        {
            Type = IssueType.Internship;
        }
        [Display(Name = "Current Semester")]
        [Range(1, 12, ErrorMessage = "Enter valid semester")]
        public int CurrentSemester { get; set; }

        [Display(Name = "Internship Type")]
        public string InternshipType { get; set; } 

        [Display(Name = "Looking for internship in")]
        public string PreferredField { get; set; }

        [Display(Name = "Company Name (if found)")]
        public string CompanyName { get; set; }

        [Display(Name = "Need guidance for")]
        public string GuidanceNeeded { get; set; } 

        [Display(Name = "Preferred internship duration")]
        public string PreferredDuration { get; set; } 

        [Display(Name = "Skills you have")]
        public string Skills { get; set; }

        [Display(Name = "Previous internship experience")]
        public bool HasPreviousExperience { get; set; }

        [Display(Name = "Previous company name")]
        public string PreviousCompany { get; set; }

    }
}
