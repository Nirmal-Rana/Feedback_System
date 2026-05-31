using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CollegeIssueManagement.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IssueId { get; set; }

        [ForeignKey("IssueId")]
        public virtual Issue? Issue { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Message { get; set; } = string.Empty;

        public bool IsRead { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ReadDate { get; set; }
    }
}