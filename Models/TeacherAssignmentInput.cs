namespace CollegeIssueManagement.Models
{
    // Shape of each object the wizard's JSON payload sends per "card".
    public class TeacherAssignmentInput
    {
        public List<string> Semesters { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public List<string> Sections { get; set; } = new();
    }
}