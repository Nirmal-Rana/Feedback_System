using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models.ViewModels
{
    
    public class InternshipIssueViewModel : BaseIssueViewModel
    {
        [Required(ErrorMessage = "Current Semester is required.")]
        public int CurrentSemester { get; set; }

        [Required(ErrorMessage = "Preferred Field / Domain is required.")]
        public string PreferredField { get; set; } = string.Empty;

        public string? PreferredDuration { get; set; }

        
        [Required(ErrorMessage = "Please upload your CV.")]
        public IFormFile CVFile { get; set; } = null!;

        public string? Skills { get; set; }

        public bool HasPreviousExperience { get; set; }

        public string? PreviousCompany { get; set; }

       
    }
}