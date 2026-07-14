using CollegeIssueManagement.Data;
using CollegeIssueManagement.Models;
using CollegeIssueManagement.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CollegeIssueManagement.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IQRCodeService _qrCodeService;

        public AdminController(
            ApplicationDbContext context,
            IEmailService emailService,
            IQRCodeService qrCodeService)
        {
            _context = context;
            _emailService = emailService;
            _qrCodeService = qrCodeService;
        }

        // ─────────────────────────────────────────────
        // LOGIN
        // ─────────────────────────────────────────────
        [HttpGet]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetString("AdminUsername") != null)
                return RedirectToAction("Dashboard");

            if (HttpContext.Session.GetString("AttendanceAdmin") != null)
                return RedirectToAction("AttendanceDashboard", "Attendance");

            return View();
        }

        // ── QR Management page ──
        public async Task<IActionResult> ManageQRCodes()
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var settings = await _context.QRCodeSettings
                .OrderBy(q => q.DisplayName)
                .ToListAsync();

            return View(settings);
        }

        // ── Toggle single QR ──
        [HttpPost]
        public async Task<IActionResult> ToggleQRCode([FromBody] ToggleQRRequest request)
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return Unauthorized();

            var setting = await _context.QRCodeSettings
                .FirstOrDefaultAsync(q => q.IssueType == request.IssueType);

            if (setting == null) return NotFound();

            setting.IsEnabled = request.Enabled;
            setting.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                issueType = request.IssueType,
                enabled = request.Enabled
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                if ((model.Username ?? "").Trim() == "attendance" &&
                    (model.Password ?? "").Trim() == "Texas@2026#1")
                {
                    HttpContext.Session.SetString("AttendanceAdmin", "true");
                    return RedirectToAction("AttendanceDashboard", "Attendance");
                }
                if ((model.Username ?? "").Trim() == "feedback" &&
                    (model.Password ?? "").Trim() == "Feedback@2026@1991")
                {
                    HttpContext.Session.SetString("FeedbackAdmin", "true");
                    return RedirectToAction("FeedbackDashboard", "Feedback");
                }

                var admin = await _context.Admins
                    .FirstOrDefaultAsync(a =>
                        a.Username == model.Username &&
                        a.Password == model.Password);

                if (admin != null)
                {
                    HttpContext.Session.SetString("AdminUsername", admin.Username);
                    HttpContext.Session.SetString("AdminId", admin.Id.ToString());
                    admin.LastLogin = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Dashboard");
                }

                ModelState.AddModelError("", "Invalid username or password");
            }
            return View(model);
        }

        // ─────────────────────────────────────────────
        // DASHBOARD
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var issues = await _context.Issues.ToListAsync();

            var notifications = await _context.Notifications
                .Include(n => n.Issue)
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .ToListAsync();

            var dashboard = new DashboardViewModel
            {
                TotalIssues = issues.Count,
                PendingIssues = issues.Count(i => i.Status == IssueStatus.Pending),
                ApprovedIssues = issues.Count(i => i.Status == IssueStatus.Approved),
                ResolvedIssues = issues.Count(i => i.Status == IssueStatus.Resolved),
                TotalNotifications = await _context.Notifications.CountAsync(),
                UnreadNotifications = await _context.Notifications.CountAsync(n => !n.IsRead),

                IssuesByType = issues
                    .GroupBy(i => i.Type.ToString())
                    .ToDictionary(g => g.Key, g => g.Count()),

                RecentIssues = issues
                    .OrderByDescending(i => i.SubmittedDate)
                    .Take(10)
                    .ToList(),

                RecentNotifications = notifications
            };

            return View(dashboard);
        }

        // ─────────────────────────────────────────────
        // ISSUES LIST
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Issues(string type = "all", string status = "all")
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var allIssues = await _context.Issues
                .OrderByDescending(i => i.SubmittedDate)
                .ToListAsync();

            IEnumerable<Issue> filtered = allIssues;

            if (type != "all" && Enum.TryParse<IssueType>(type, out IssueType parsedType))
                filtered = filtered.Where(i => i.Type == parsedType);

            if (status != "all" && Enum.TryParse<IssueStatus>(status, out IssueStatus parsedStatus))
                filtered = filtered.Where(i => i.Status == parsedStatus);

            ViewBag.CurrentType = type;
            ViewBag.CurrentStatus = status;

            return View(filtered.ToList());
        }

        // ─────────────────────────────────────────────
        // ISSUE DETAILS
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> IssueDetails(int id)
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var issue = await _context.Issues.FindAsync(id);
            if (issue == null) return NotFound();

            return View(issue);
        }

        // ─────────────────────────────────────────────
        // UPDATE STATUS
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssueStatus(int id, string status, string remarks)
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Unauthorized." });
                return RedirectToAction("Login");
            }

            if (!Enum.TryParse<IssueStatus>(status, true, out IssueStatus newStatus))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Invalid status." });
                TempData["Error"] = "Invalid status value.";
                return RedirectToAction("IssueDetails", new { id });
            }

            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = false, message = "Issue not found." });
                return NotFound();
            }

            var oldStatus = issue.Status;
            issue.Status = newStatus;

            if (newStatus == IssueStatus.Resolved && issue.ResolvedDate == null)
                issue.ResolvedDate = DateTime.Now;

            if (!string.IsNullOrEmpty(remarks))
                issue.AdminRemarks = remarks;

            await _context.SaveChangesAsync();

            _context.Notifications.Add(new Notification
            {
                IssueId = issue.Id,
                Title = "Issue Status Updated: " + newStatus,
                Message = "Your " + issue.Type + " issue changed from " + oldStatus + " to " + newStatus +
                          (string.IsNullOrEmpty(remarks) ? "" : ". Remarks: " + remarks),
                CreatedDate = DateTime.Now,
                IsRead = false
            });
            await _context.SaveChangesAsync();

            try
            {
                string remarksRow = string.IsNullOrEmpty(remarks) ? "" :
                    "<tr style='background:#f8f9ff;'><td style='padding:10px;font-weight:bold;'>Remarks</td>" +
                    "<td style='padding:10px;'>" + remarks + "</td></tr>";

                string emailBody =
                    "<html><body style='font-family:Arial,sans-serif;padding:20px;'>" +
                    "<h2 style='color:#667eea;'>Issue Status Update</h2>" +
                    "<p>Dear <strong>" + issue.StudentName + "</strong>,</p>" +
                    "<p>Your issue <strong>#" + issue.Id + " (" + issue.Type + ")</strong> status has been updated.</p>" +
                    "<table style='border-collapse:collapse;width:100%;margin:16px 0;'>" +
                    "<tr style='background:#f8f9ff;'><td style='padding:10px;font-weight:bold;'>Previous Status</td>" +
                    "<td style='padding:10px;'>" + oldStatus + "</td></tr>" +
                    "<tr><td style='padding:10px;font-weight:bold;'>New Status</td>" +
                    "<td style='padding:10px;font-weight:bold;color:#667eea;'>" + newStatus + "</td></tr>" +
                    remarksRow + "</table>" +
                    "<p style='color:#888;font-size:12px;'>— Texas College Issue Management System</p>" +
                    "</body></html>";

                await _emailService.SendEmailAsync(
                    issue.StudentEmail,
                    "Issue Update #" + issue.Id + ": " + issue.Type + " — " + newStatus,
                    emailBody);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Email failed: " + ex.Message);
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    message = "Status updated from " + oldStatus + " to " + newStatus + ".",
                    newStatus = newStatus.ToString(),
                    oldStatus = oldStatus.ToString()
                });
            }

            TempData["Success"] = "Issue status updated from " + oldStatus + " to " + newStatus + " successfully.";
            return RedirectToAction("IssueDetails", new { id });
        }

        // ─────────────────────────────────────────────
        // DELETE SINGLE ISSUE
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIssue(int id)
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var issue = await _context.Issues.FindAsync(id);
            if (issue == null) return NotFound();

            var related = _context.Notifications.Where(n => n.IssueId == id);
            _context.Notifications.RemoveRange(related);
            _context.Issues.Remove(issue);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Issue #" + id + " (" + issue.Type + ") by " + issue.StudentName + " deleted successfully.";
            return RedirectToAction("Issues");
        }

        // ─────────────────────────────────────────────
        // BULK DELETE REJECTED
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDeleteRejected()
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var rejected = await _context.Issues
                .Where(i => i.Status == IssueStatus.Rejected)
                .ToListAsync();

            if (!rejected.Any())
            {
                TempData["Error"] = "No rejected issues found to delete.";
                return RedirectToAction("Issues");
            }

            var ids = rejected.Select(i => i.Id).ToList();
            var relatedNotifs = _context.Notifications.Where(n => ids.Contains(n.IssueId));
            _context.Notifications.RemoveRange(relatedNotifs);
            _context.Issues.RemoveRange(rejected);
            await _context.SaveChangesAsync();

            TempData["Success"] = rejected.Count + " rejected issue(s) deleted successfully.";
            return RedirectToAction("Issues");
        }

        // ─────────────────────────────────────────────
        // BULK DELETE BY FILTER
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(string type, string status)
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var issues = await _context.Issues.ToListAsync();

            if (!string.IsNullOrEmpty(type) && type != "all" && Enum.TryParse<IssueType>(type, out IssueType parsedType))
                issues = issues.Where(i => i.Type == parsedType).ToList();

            if (!string.IsNullOrEmpty(status) && status != "all" && Enum.TryParse<IssueStatus>(status, out IssueStatus parsedStatus))
                issues = issues.Where(i => i.Status == parsedStatus).ToList();

            if (!issues.Any())
            {
                TempData["Error"] = "No issues found matching the selected filters.";
                return RedirectToAction("Issues");
            }

            var ids = issues.Select(i => i.Id).ToList();
            var relatedNotifs = _context.Notifications.Where(n => ids.Contains(n.IssueId));
            _context.Notifications.RemoveRange(relatedNotifs);
            _context.Issues.RemoveRange(issues);
            await _context.SaveChangesAsync();

            TempData["Success"] = issues.Count + " issue(s) deleted successfully.";
            return RedirectToAction("Issues");
        }

        // ─────────────────────────────────────────────
        // NOTIFICATIONS
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Notifications()
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var notifications = await _context.Notifications
                .Include(n => n.Issue)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // ─────────────────────────────────────────────
        // GENERATE REPORT
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GenerateReport(string fromDate, string toDate, string issueType)
        {
            if (HttpContext.Session.GetString("AdminUsername") == null)
                return RedirectToAction("Login");

            var issues = await _context.Issues.ToListAsync();

            if (!string.IsNullOrEmpty(fromDate) && DateTime.TryParse(fromDate, out DateTime from))
                issues = issues.Where(i => i.SubmittedDate >= from).ToList();

            if (!string.IsNullOrEmpty(toDate) && DateTime.TryParse(toDate, out DateTime to))
                issues = issues.Where(i => i.SubmittedDate <= to).ToList();

            if (!string.IsNullOrEmpty(issueType) && issueType != "all" && Enum.TryParse<IssueType>(issueType, out IssueType parsedType))
                issues = issues.Where(i => i.Type == parsedType).ToList();

            var report = new
            {
                TotalIssues = issues.Count,
                ByStatus = issues.GroupBy(i => i.Status).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                ByType = issues.GroupBy(i => i.Type).ToDictionary(g => g.Key.ToString(), g => g.Count()),
                Issues = issues.Select(i => new
                {
                    i.Id,
                    i.StudentName,
                    i.StudentRollNo,
                    i.Type,
                    i.Status,
                    i.SubmittedDate,
                    i.ResolvedDate
                })
            };

            return Json(report);
        }

        // ─────────────────────────────────────────────
        // LOGOUT
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

            foreach (var cookie in Request.Cookies.Keys)
                Response.Cookies.Delete(cookie);

            return RedirectToAction("Index", "Home");
        }

        public class ToggleQRRequest
        {
            public string IssueType { get; set; } = string.Empty;
            public bool Enabled { get; set; }
        }
    }
}