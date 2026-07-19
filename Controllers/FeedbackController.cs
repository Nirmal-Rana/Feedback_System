using CollegeIssueManagement.Data;
using CollegeIssueManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace CollegeIssueManagement.Controllers
{
    [AllowAnonymous]
    public class FeedbackController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(
            ApplicationDbContext context,
            ILogger<FeedbackController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ═══════════════════════════════════════
        // STUDENT — Submit Feedback Form
        // ═══════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> SubmitFeedback()
        {
            // Fetch raw semesters, split comma-separated selections, and return a clean distinct list
            var rawSemesters = await _context.Teachers
                .Where(t => t.IsActive)
                .Select(t => t.Semester)
                .ToListAsync();

            var semesters = rawSemesters
                .SelectMany(s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                .Select(s => s.Trim())
                .Where(s => s != "ALL")
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            ViewBag.Semesters = semesters;
            return View();
        }

        // ── API: Get subjects by semester ──
        [HttpGet]
        public async Task<IActionResult> GetSubjectsBySemester(string semester)
        {
            // Pull active teachers matching selected semester or "ALL"
            var teachers = await _context.Teachers
                .Where(t => t.IsActive && (t.Semester.Contains(semester) || t.Semester.Contains("ALL")))
                .ToListAsync();

            // Flatten multi-assigned sections so students see individual clean section options
            var subjects = teachers
                .SelectMany(t => {
                    var classes = string.IsNullOrEmpty(t.ProfessionalClass)
                        ? new[] { "" }
                        : t.ProfessionalClass.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim());

                    return classes.Select(c => new { t.Subject, ProfessionalClass = c });
                })
                .Distinct()
                .OrderBy(t => t.Subject)
                .ToList();

            return Json(subjects);
        }

        // ── API: Get ALL teachers by semester + subject + professionalClass ──
        [HttpGet]
        public async Task<IActionResult> GetTeachersBySubject(
            string semester, string subject, string? professionalClass)
        {
            var query = _context.Teachers.Where(t => t.IsActive && t.Subject == subject);
            var teachersList = await query.ToListAsync();

            // Filter in-memory to securely evaluate comma-separated list targets
            var filtered = teachersList
                .Where(t =>
                    (t.Semester.Split(',').Select(s => s.Trim()).Contains(semester) || t.Semester.Contains("ALL")) &&
                    (string.IsNullOrEmpty(professionalClass) ||
                     (t.ProfessionalClass != null && t.ProfessionalClass.Split(',').Select(c => c.Trim()).Contains(professionalClass)))
                )
                .Select(t => new { t.Id, t.FullName, t.Designation })
                .OrderBy(t => t.FullName)
                .ToList();

            return Json(filtered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(TeacherFeedback model)
        {
            if (!ModelState.IsValid)
            {
                var rawSemesters = await _context.Teachers
                    .Where(t => t.IsActive)
                    .Select(t => t.Semester)
                    .ToListAsync();

                var semesters = rawSemesters
                    .SelectMany(s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    .Select(s => s.Trim())
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                ViewBag.Semesters = semesters;
                return View(model);
            }

            try
            {
                model.SubmittedDate = DateTime.Now;
                _context.TeacherFeedbacks.Add(model);
                await _context.SaveChangesAsync();

                TempData["FeedbackSuccess"] = "true";
                TempData["FeedbackId"] = model.Id.ToString();
                TempData["TeacherName"] =
                    (await _context.Teachers.FindAsync(model.TeacherId))
                    ?.FullName ?? "";

                return RedirectToAction("FeedbackConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving feedback");
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return View(model);
            }
        }

        public IActionResult FeedbackConfirmation()
        {
            if (TempData["FeedbackSuccess"] == null)
                return RedirectToAction("SubmitFeedback");
            return View();
        }

        // ═══════════════════════════════════════
        // ADMIN — Dashboard
        // ═══════════════════════════════════════

        public async Task<IActionResult> FeedbackDashboard(
            string? teacherFilter,
            string? ratingFilter,
            string? semesterFilter,
            int page = 1)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var activeFeedbacksQuery = _context.TeacherFeedbacks
                .Include(f => f.Teacher)
                .Where(f => f.Teacher != null && f.Teacher.IsActive);

            // ── Stats ──
            ViewBag.TotalCount = await activeFeedbacksQuery.CountAsync();
            ViewBag.TodayCount = await activeFeedbacksQuery.CountAsync(f => f.SubmittedDate.Date == today);
            ViewBag.WeekCount = await activeFeedbacksQuery.CountAsync(f => f.SubmittedDate.Date >= weekStart);
            ViewBag.MonthCount = await activeFeedbacksQuery.CountAsync(f => f.SubmittedDate.Date >= monthStart);
            ViewBag.PendingCount = await activeFeedbacksQuery.CountAsync(f => f.IsReviewed);

            // ── Rating distribution ──
            var allFeedbacks = await activeFeedbacksQuery.ToListAsync();
            ViewBag.ExcellentCount = allFeedbacks.Count(f => f.Rating == "Excellent");
            ViewBag.GoodCount = allFeedbacks.Count(f => f.Rating == "Good");
            ViewBag.AverageCount = allFeedbacks.Count(f => f.Rating == "Average");
            ViewBag.BelowAvgCount = allFeedbacks.Count(f => f.Rating == "Below Average");

            // ── Teacher list for filter ──
            ViewBag.Teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.Semesters = await activeFeedbacksQuery
                .Select(f => f.Semester).Distinct().ToListAsync();

            // ── Per-teacher summary ──
            var teacherSummary = await _context.Teachers
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.FullName,
                    t.Subject,
                    t.Semester,
                    Total = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id),
                    Excellent = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Excellent"),
                    Good = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Good"),
                    Average = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Average"),
                    BelowAvg = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Below Average")
                })
                .ToListAsync();

            ViewBag.TeacherSummary = teacherSummary;

            // ── Apply filters to the table data ──
            var query = activeFeedbacksQuery.AsQueryable();

            if (!string.IsNullOrEmpty(teacherFilter) && int.TryParse(teacherFilter, out int tid))
                query = query.Where(f => f.TeacherId == tid);

            if (!string.IsNullOrEmpty(ratingFilter))
                query = query.Where(f => f.Rating == ratingFilter);

            if (!string.IsNullOrEmpty(semesterFilter))
                query = query.Where(f => f.Semester == semesterFilter);

            int pageSize = 15;
            int totalRecords = await query.CountAsync();
            var feedbacks = await query
                .OrderByDescending(f => f.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
            ViewBag.TotalFiltered = totalRecords;
            ViewBag.TeacherFilter = teacherFilter;
            ViewBag.RatingFilter = ratingFilter;
            ViewBag.SemesterFilter = semesterFilter;

            return View(feedbacks);
        }

        // ── Individual teacher detail ──
        public async Task<IActionResult> TeacherDetail(int id)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            ViewBag.TeacherName = teacher.FullName;
            ViewBag.Subject = teacher.Subject;
            ViewBag.Semester = teacher.Semester;

            var feedbacks = await _context.TeacherFeedbacks
                                          .Where(f => f.TeacherId == id)
                                          .OrderByDescending(f => f.SubmittedDate)
                                          .ToListAsync();

            int totalCount = feedbacks.Count;
            int excellentCount = feedbacks.Count(f => f.Rating == "Excellent");
            int goodCount = feedbacks.Count(f => f.Rating == "Good");
            int averageCount = feedbacks.Count(f => f.Rating == "Average");
            int belowAvgCount = feedbacks.Count(f => f.Rating == "Below Average");

            ViewBag.TotalCount = totalCount;
            ViewBag.ExcellentCount = excellentCount;
            ViewBag.GoodCount = goodCount;
            ViewBag.AverageCount = averageCount;
            ViewBag.BelowAvgCount = belowAvgCount;

            return View(feedbacks);
        }

        // ── Add Teacher (Updated to accept arrays for Multi-Selection) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(string FullName, string Subject, string[] Semesters, string[] Sections, string Designation)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            // Validate Text Fields Manually to bypass standard model validation state errors
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Subject))
            {
                TempData["Error"] = "Full Name and Subject are required fields.";
                return RedirectToAction("ManageTeachers");
            }

            // Ensure selection choices are sent correctly
            if (Semesters == null || Semesters.Length == 0 || Sections == null || Sections.Length == 0)
            {
                TempData["Error"] = "Please select at least one Semester and one Section.";
                return RedirectToAction("ManageTeachers");
            }

            try
            {
                // Join array lists into persistent comma-separated strings
                string combinedSemesters = string.Join(", ", Semesters);
                string combinedSections = string.Join(", ", Sections);

                var teacher = new Teacher
                {
                    FullName = FullName.Trim(),
                    Subject = Subject.Trim(),
                    Semester = combinedSemesters, // Holds data like: "1st Semester, 2nd Semester"
                    ProfessionalClass = combinedSections, // Holds mapped sections: "A, B"
                    Designation = string.IsNullOrWhiteSpace(Designation) ? "Lecturer" : Designation.Trim(),
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.Teachers.Add(teacher);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Teacher {teacher.FullName} added successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing AddTeacher");
                TempData["Error"] = "An error occurred while saving: " + ex.Message;
            }

            return RedirectToAction("ManageTeachers");
        }

        // ── Delete Teacher ──
        [HttpPost]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                teacher.IsActive = false; // Soft delete
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Teacher removed successfully.";
            return RedirectToAction("ManageTeachers");
        }

        // ── Manage Teachers page ──
        [AllowAnonymous]
        public async Task<IActionResult> ManageTeachers()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .OrderBy(t => t.Semester)
                .ThenBy(t => t.FullName)
                .ToListAsync();

            return View(teachers);
        }

        // ── Dedicated Teachers List Route ──
        [AllowAnonymous]
        public async Task<IActionResult> TeachersList()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .OrderBy(t => t.FullName)
                .ToListAsync();

            // Get feedback count per teacher
            var feedbackCounts = await _context.TeacherFeedbacks
                .GroupBy(f => f.TeacherId)
                .Select(g => new {
                    TeacherId = g.Key,
                    Count = g.Count(),
                    Excellent = g.Count(f => f.Rating == "Excellent")
                })
                .ToListAsync();

            var countsDict = new Dictionary<int, dynamic>();
            foreach (var f in feedbackCounts)
            {
                countsDict[f.TeacherId] = new
                {
                    Count = f.Count,
                    Excellent = f.Excellent
                };
            }

            ViewBag.FeedbackCounts = countsDict;

            return View(teachers);
        }

        // ── Delete single feedback ──
        [HttpPost]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var feedback = await _context.TeacherFeedbacks.FindAsync(id);
            if (feedback != null)
            {
                _context.TeacherFeedbacks.Remove(feedback);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("FeedbackDashboard");
        }

        // ── Bulk delete feedbacks ──
        [HttpPost]
        public async Task<IActionResult> BulkDeleteFeedback([FromBody] List<int> ids)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return Unauthorized();

            if (ids == null || !ids.Any()) return BadRequest();

            var feedbacks = await _context.TeacherFeedbacks
                .Where(f => ids.Contains(f.Id))
                .ToListAsync();

            _context.TeacherFeedbacks.RemoveRange(feedbacks);
            await _context.SaveChangesAsync();

            return Ok(new { deleted = feedbacks.Count });
        }

        // ── Export CSV ──
        public async Task<IActionResult> ExportFeedback(int? teacherId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var query = _context.TeacherFeedbacks
                .Include(f => f.Teacher)
                .AsQueryable();

            if (teacherId.HasValue)
                query = query.Where(f => f.TeacherId == teacherId);

            var feedbacks = await query
                .OrderByDescending(f => f.SubmittedDate)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Teacher,Subject,Semester,Student,LCID,Rating,Feedback,Anonymous,Date,Reviewed");

            foreach (var f in feedbacks)
            {
                csv.AppendLine(
                    $"{f.Id}," +
                    $"\"{f.Teacher?.FullName ?? ""}\"," +
                    $"\"{f.Subject}\"," +
                    $"\"{f.Semester}\"," +
                    $"\"{(f.IsAnonymous ? "Anonymous" : f.StudentName)}\"," +
                    $"\"{f.LCID}\"," +
                    $"\"{f.Rating}\"," +
                    $"\"{f.FeedbackText.Replace("\"", "'")}\"," +
                    $"{(f.IsAnonymous ? "Yes" : "No")}," +
                    $"{f.SubmittedDate:dd/MM/yyyy HH:mm}," +
                    $"{(f.IsReviewed ? "Yes" : "No")}");
            }

            return File(
                Encoding.UTF8.GetBytes("\uFEFF" + csv.ToString()),
                "text/csv",
                $"Feedback_{DateTime.Now:yyyyMMdd}.csv");
        }

        // ── Feedback Logout ──
        public IActionResult FeedbackLogout()
        {
            if (HttpContext.Session.Keys.Contains("FeedbackAdmin"))
            {
                HttpContext.Session.Remove("FeedbackAdmin");
            }
            return RedirectToAction("Index", "Home");
        }
    }
}