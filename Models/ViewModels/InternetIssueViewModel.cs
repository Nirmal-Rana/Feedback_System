using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class InternetIssueViewModel : BaseIssueViewModel
    {
        public InternetIssueViewModel()
        {
            Type = IssueType.Internet;
        }

        [Display(Name = "Internet Type")]
        public string InternetType { get; set; } = string.Empty;

        [Display(Name = "Issue Category")]
        public string IssueCategory { get; set; } = string.Empty;

        [Display(Name = "Location/Building")]
        public string Location { get; set; } = string.Empty;

        [Display(Name = "Room/Floor Number")]
        public string RoomNumber { get; set; } = string.Empty;

        //[Display(Name = "Device Type")]
        //public string DeviceType { get; set; } = string.Empty;

        //[Display(Name = "Operating System")]
        //public string OperatingSystem { get; set; } = string.Empty;

        [Display(Name = "Your Program Name BIT/BCS")]
        public string Program { get; set; } = string.Empty;

        [Display(Name = "Your Student LC id")]
        public string Lcid { get; set; } = string.Empty;

        [Display(Name = "Your Section Eg A, B, C")]
        public string Section { get; set; } = string.Empty;

        [Display(Name = "Your Phone Number")]
        public string Contact { get; set; } = string.Empty;

        [Display(Name = "When did the issue start?")]
        public DateTime? IssueStartDate { get; set; }

        //[Display(Name = "Is the issue recurring?")]
        //public bool IsRecurring { get; set; }

        [Display(Name = "Screenshot/Error Message")]
        public string ErrorMessage { get; set; } = string.Empty;
    }
}