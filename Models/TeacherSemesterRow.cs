namespace CollegeIssueManagement.Models
{
    // Flattened view of a teacher scoped to one semester — used by TeachersBySemester.
    public class TeacherSemesterRow
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? PhotoPath { get; set; }
        public string? Designation { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string? Section { get; set; }
    }
}