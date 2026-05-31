using CollegeIssueManagement.Data;
using CollegeIssueManagement.Models;
using CollegeIssueManagement.Models.ViewModels;
using CollegeIssueManagement.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CollegeIssueManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IQRCodeService _qrCodeService;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            ApplicationDbContext context,
            IQRCodeService qrCodeService,
            IWebHostEnvironment hostEnvironment,
            ILogger<HomeController> logger)
        {
            _context = context;
            _qrCodeService = qrCodeService;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult CreateIssue(string type)
        {
            if (string.IsNullOrEmpty(type))
            {
                return RedirectToAction("Index");
            }

            ViewBag.IssueType = type;

            BaseIssueViewModel model = type switch
            {
                "Internet" => new InternetIssueViewModel(),
                "IDCard" => new IDCardIssueViewModel(),
                "Internship" => new InternshipIssueViewModel(),
                "JobOpportunity" => new JobOpportunityViewModel(),
                "FacultyFeedback" => new FacultyFeedbackViewModel(),
                "Counselling" => new CounsellingViewModel(),
                "ManagementAttention" => new ManagementAttentionViewModel(),
                "Others" => new BaseIssueViewModel { Type = IssueType.Others },
                _ => new BaseIssueViewModel { Type = IssueType.Others }
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIssue(BaseIssueViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IssueType = model.Type.ToString();
                return View(model);
            }

            try
            {
                // Map view model to Issue entity
                var issue = new Issue
                {
                    StudentName = model.StudentName,
                    StudentEmail = model.StudentEmail,
                    StudentRollNo = model.StudentRollNo,
                    StudentDepartment = model.StudentDepartment,
                    StudentPhone = model.StudentPhone,
                    Type = model.Type,
                    Description = model.Description,
                    //Status = IssueStatus.Pending,
                    SubmittedDate = DateTime.Now,
                    AdditionalData = JsonSerializer.Serialize(model) // Store complete form data
                };

                // Generate QR code for tracking
                var qrData = JsonSerializer.Serialize(new
                {
                    issueId = issue.Id,
                    type = issue.Type.ToString(),
                    studentEmail = issue.StudentEmail
                });
                issue.QRCodeData = _qrCodeService.GenerateQRCode(qrData);

                _context.Issues.Add(issue);
                await _context.SaveChangesAsync();

                // Create notification for admin
                var notification = new Notification
                {
                    IssueId = issue.Id,
                    Title = $"New {issue.Type} Issue Submitted",
                    Message = $"Student {issue.StudentName} ({issue.StudentRollNo}) submitted a new issue.",
                    CreatedDate = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Issue submitted successfully! You will receive email notification once reviewed.";
                TempData["IssueId"] = issue.Id;

                return RedirectToAction("IssueConfirmation", new { id = issue.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting issue");
                ModelState.AddModelError("", "An error occurred while submitting your issue. Please try again.");
                ViewBag.IssueType = model.Type.ToString();
                return View(model);
            }
        }

        public async Task<IActionResult> IssueConfirmation(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null)
            {
                return NotFound();
            }
            return View(issue);
        }

        [HttpGet]
        public IActionResult ScanQR()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessQRScan(string qrData)
        {
            if (string.IsNullOrEmpty(qrData))
            {
                TempData["Error"] = "Invalid QR Code";
                return RedirectToAction("ScanQR");
            }

            try
            {
                var qrInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(qrData);
                if (qrInfo.ContainsKey("type"))
                {
                    return RedirectToAction("CreateIssue", new { type = qrInfo["type"] });
                }
                else if (qrInfo.ContainsKey("issueId"))
                {
                    var issueId = int.Parse(qrInfo["issueId"]);
                    var issue = await _context.Issues.FindAsync(issueId);
                    if (issue != null)
                    {
                        return RedirectToAction("IssueStatus", new { id = issueId });
                    }
                }

                TempData["Error"] = "Invalid QR Code Format";
                return RedirectToAction("ScanQR");
            }
            catch
            {
                TempData["Error"] = "Invalid QR Code Format";
                return RedirectToAction("ScanQR");
            }
        }

        public async Task<IActionResult> IssueStatus(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Notifications)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null)
            {
                return NotFound();
            }

            return View(issue);
        }
    }
}