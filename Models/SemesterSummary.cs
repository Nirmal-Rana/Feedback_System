namespace CollegeIssueManagement.Models
{
    public class SemesterSummary
    {
        public string Semester { get; set; } = string.Empty;
        public int TeacherCount { get; set; }
        public int FeedbackCount { get; set; }
    }
}