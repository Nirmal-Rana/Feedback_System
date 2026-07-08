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
                return RedirectToAction("Index");

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
        // ── API for Index.cshtml to get enabled QR types ──
        [HttpGet]
        public async Task<IActionResult> GetEnabledQRCodes()
        {
            try
            {
                var enabled = await _context.QRCodeSettings
                    .Where(q => q.IsEnabled)
                    .Select(q => q.IssueType)
                    .ToListAsync();

                // ✅ If table is empty, return ALL types so page doesn't break
                if (!enabled.Any())
                {
                    enabled = new List<string>
            {
                "Internet", "IDCard", "Internship", "JobOpportunity",
                "TeacherFeedback", "ManagementAttention", "Counselling",
                "Others", "Absence"
            };
                }

                return Json(enabled);
            }
            catch
            {
                // ✅ If table doesn't exist yet, return all types
                return Json(new List<string>
        {
            "Internet", "IDCard", "Internship", "JobOpportunity",
            "TeacherFeedback", "ManagementAttention", "Counselling",
            "Others", "Absence"
        });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIssue(BaseIssueViewModel model)
        {
           
            var baseProps = typeof(BaseIssueViewModel)
                .GetProperties()
                .Select(p => p.Name)
                .ToHashSet();

            foreach (var key in ModelState.Keys.ToList())
            {
                if (!baseProps.Contains(key))
                    ModelState.Remove(key);
            }

            // ── 2. Build Description from AdditionalData if empty ──
            // Sub-forms (Internet, IDCard etc.) post extra fields.
            // We collect them into AdditionalData and use them to
            // build the Description string when it is blank.
            if (string.IsNullOrWhiteSpace(model.Description))
            {
                var extras = Request.Form
                    .Where(f => !baseProps.Contains(f.Key)
                                && f.Key != "__RequestVerificationToken")
                    .ToDictionary(f => f.Key, f => f.Value.ToString());

                if (extras.Count > 0)
                {
                    model.Description = string.Join(" | ",
                        extras.Select(kv => $"{kv.Key}: {kv.Value}"));
                }
            }

            // ── 3. Re-validate after fixes ──
            ModelState.Remove(nameof(model.Description));
            if (string.IsNullOrWhiteSpace(model.Description))
                ModelState.AddModelError(nameof(model.Description),
                    "Description is required.");

            if (!ModelState.IsValid)
            {
                ViewBag.IssueType = model.Type.ToString();
                return View(model);
            }

            try
            {
                // ── 4. Collect extra form fields as JSON for AdditionalData ──
                var extraFields = Request.Form
                    .Where(f => !baseProps.Contains(f.Key)
                                && f.Key != "__RequestVerificationToken")
                    .ToDictionary(f => f.Key, f => f.Value.ToString());

                var issue = new Issue
                {
                    StudentName = model.StudentName,
                    StudentEmail = model.StudentEmail,
                    StudentRollNo = model.StudentRollNo,
                    StudentDepartment = model.StudentDepartment,
                    StudentPhone = model.StudentPhone,
                    Type = model.Type,
                    Description = model.Description,
                    SubmittedDate = DateTime.Now,
                    AdditionalData = extraFields.Count > 0
                                            ? JsonSerializer.Serialize(extraFields)
                                            : null
                };

                // ── 5. Save FIRST so we get the real issue.Id ──
                _context.Issues.Add(issue);
                await _context.SaveChangesAsync();

                // ── 6. Generate QR code with a real tracking URL ──
                var trackingUrl = Url.Action(
                    "IssueStatus", "Home",
                    new { id = issue.Id },
                    Request.Scheme);          // e.g. https://tcmitinfo.net/Home/IssueStatus?id=5

                issue.QRCodeData = _qrCodeService.GenerateQRCode(trackingUrl);
                _context.Issues.Update(issue);
                await _context.SaveChangesAsync();

                // ── 7. Create admin notification ──
                var notification = new Notification
                {
                    IssueId = issue.Id,
                    Title = $"New {issue.Type} Issue Submitted",
                    Message = $"Student {issue.StudentName} ({issue.StudentRollNo}) " +
                                  $"submitted a new {issue.Type} issue.",
                    CreatedDate = DateTime.Now
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Issue submitted successfully! " +
                                      "You will receive an email notification once reviewed.";
                TempData["IssueId"] = issue.Id;

                return RedirectToAction("IssueConfirmation", new { id = issue.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting issue for student {Email}",
                    model.StudentEmail);
                ModelState.AddModelError("",
                    "An error occurred while submitting your issue. Please try again.");
                ViewBag.IssueType = model.Type.ToString();
                return View(model);
            }
        }

        public async Task<IActionResult> IssueConfirmation(int id)
        {
            var issue = await _context.Issues.FindAsync(id);
            if (issue == null) return NotFound();
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
                // ── New: QR now contains a plain URL ──
                // e.g. https://tcmitinfo.net/Home/IssueStatus?id=5
                if (qrData.StartsWith("http://") || qrData.StartsWith("https://"))
                {
                    var uri = new Uri(qrData);
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);

                    // Tracking QR → go to issue status page
                    if (query["id"] != null && int.TryParse(query["id"], out int issueId))
                    {
                        var issue = await _context.Issues.FindAsync(issueId);
                        if (issue != null)
                            return RedirectToAction("IssueStatus", new { id = issueId });
                    }

                    // Form QR → go to issue creation form
                    if (query["type"] != null)
                        return RedirectToAction("CreateIssue", new { type = query["type"] });
                }

                // ── Legacy: handle old JSON-format QR codes ──
                if (qrData.TrimStart().StartsWith("{"))
                {
                    var qrInfo = JsonSerializer.Deserialize<Dictionary<string, string>>(qrData);

                    if (qrInfo != null && qrInfo.TryGetValue("type", out var type))
                        return RedirectToAction("CreateIssue", new { type });

                    if (qrInfo != null && qrInfo.TryGetValue("issueId", out var idStr)
                        && int.TryParse(idStr, out int legacyId))
                    {
                        var issue = await _context.Issues.FindAsync(legacyId);
                        if (issue != null)
                            return RedirectToAction("IssueStatus", new { id = legacyId });
                    }
                }

                TempData["Error"] = "Invalid QR Code Format";
                return RedirectToAction("ScanQR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing QR scan");
                TempData["Error"] = "Invalid QR Code Format";
                return RedirectToAction("ScanQR");
            }
        }

        public async Task<IActionResult> IssueStatus(int id)
        {
            var issue = await _context.Issues
                .Include(i => i.Notifications)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (issue == null) return NotFound();
            return View(issue);
        }
    }
}