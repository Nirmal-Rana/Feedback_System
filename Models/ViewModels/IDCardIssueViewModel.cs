using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class IDCardIssueViewModel :BaseIssueViewModel
    {
        public IDCardIssueViewModel()
        {
            Type = IssueType.IDCard;
        }
        [Display(Name = "ID Card Number")]
        public string IDCardNumber { get; set; }

        [Display(Name = "Issue Type")]
        public string IDCardIssue { get; set; } 

        [Display(Name = "Date of Loss/Damage")]
        public DateTime? IncidentDate { get; set; }
        
        [Display(Name = "Alternative identification")]
        public string AlternativeID { get; set; }

    }
}
