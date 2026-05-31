
namespace CollegeIssueManagement.Models
{
    public class DashboardViewModel
    {
        public int TotalIssues { get; set; }
        public int PendingIssues { get; set; }
        public int ApprovedIssues { get; set; }
        public int ResolvedIssues { get; set; }
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }

        // FIXED: Changed from Dictionary<IssueType, int> to Dictionary<string, int>
        public Dictionary<string, int> IssuesByType { get; set; }

        // Recent issues
        public List<Issue> RecentIssues { get; set; }

        // Recent notifications
        public List<Notification> RecentNotifications { get; set; }
    }
}
