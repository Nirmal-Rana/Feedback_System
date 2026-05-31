using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CollegeIssueManagement.Models
{
    public class DocumentAttachment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int IssueId { get; set; }

        [ForeignKey("IssueId")]
        public virtual Issue? Issue { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [StringLength(100)]
        public string FileType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public DateTime UploadedDate { get; set; } = DateTime.Now;
    }
}