using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class FacultyFeedbackViewModel : BaseIssueViewModel
    {
        public FacultyFeedbackViewModel()
        {
            Type = IssueType.FacultyFeedback;
        }

        [Display(Name = "Faculty Name")]
        public string FacultyName { get; set; }

        [Display(Name = "Subject/Course")]
        public string CourseName { get; set; }

        [Display(Name = "Feedback Type")]
        public string FeedbackType { get; set; } // Positive, Constructive, Suggestion

        [Display(Name = "Rating (1-5)")]
        [Range(1, 5)]
        public int? Rating { get; set; }

        [Display(Name = "Specific feedback")]
        public string SpecificFeedback { get; set; }

        [Display(Name = "Would you like to remain anonymous?")]
        public bool IsAnonymous { get; set; }
    }
}
