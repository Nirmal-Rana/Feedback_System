using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    public class BaseIssueViewModel
    {
        [Required(ErrorMessage = "Student name is required")]
        [Display(Name = "Full Name")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Name must be between 3 and 100 characters")]
        public string StudentName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string StudentEmail { get; set; }

        [Required(ErrorMessage = "Roll number is required")]
        [Display(Name = "Roll Number")]
        [RegularExpression(@"^[A-Za-z0-9-]+$", ErrorMessage = "Invalid roll number format")]
        public string StudentRollNo { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Display(Name = "Department")]
        public string StudentDepartment { get; set; }

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [Display(Name = "Phone Number")]
        public string StudentPhone { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [Display(Name = "Issue Description")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters")]
        public string Description { get; set; }

        public IssueType Type { get; set; }
        public DateTime SubmittedDate { get; set; } = DateTime.Now;
    }
}
