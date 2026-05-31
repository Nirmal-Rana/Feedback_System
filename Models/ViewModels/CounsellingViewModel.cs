using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class CounsellingViewModel : BaseIssueViewModel
    {
        public CounsellingViewModel()
        {
            Type = IssueType.Counselling;
        }
        [Display(Name = "Counselling Type")]
        public string CounsellingType { get; set; } // Academic, Career, Personal, Mental Health

        [Display(Name = "Preferred Date & Time")]
        public DateTime? PreferredDateTime { get; set; }

        [Display(Name = "Available for immediate session?")]
        public bool ImmediateSessionNeeded { get; set; }

        [Display(Name = "Previous counselling experience")]
        public bool HasPreviousCounselling { get; set; }

        [Display(Name = "Specific concerns")]
        public string SpecificConcerns { get; set; }

        [Display(Name = "Emergency contact name")]
        public string EmergencyContactName { get; set; }

        [Display(Name = "Emergency contact number")]
        public string EmergencyContactNumber { get; set; }

        [Display(Name = "Is this urgent?")]
        public bool IsUrgent { get; set; }

        [Display(Name = "Additional information")]
        public string AdditionalInfo { get; set; }

    }
}
