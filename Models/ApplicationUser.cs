using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CollegeIssueManagement.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Department { get; set; }

        [StringLength(50)]
        public string? RollNumber { get; set; }

        public bool IsStudent { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? LastLoginAt { get; set; }

        public string? TwoFactorSecret { get; set; }

        // FIX: add 'new' to intentionally hide the inherited member
        public new bool TwoFactorEnabled { get; set; } = false;
    }
}