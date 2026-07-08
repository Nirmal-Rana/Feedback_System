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
            // Get distinct semesters for dropdown
            var semesters = await _context.Teachers
                .Where(t => t.IsActive)
                .Select(t => t.Semester)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();

            ViewBag.Semesters = semesters;
            return View();
        }

        // ── API: Get subjects by semester ──
        [HttpGet]
        public async Task<IActionResult> GetSubjectsBySemester(string semester)
        {
            var subjects = await _context.Teachers
                .Where(t => t.IsActive && t.Semester == semester)
                .Select(t => new {
                    t.Subject,
                    t.ProfessionalClass
                })
                .Distinct()
                .OrderBy(t => t.Subject)
                .ToListAsync();

            return Json(subjects);
        }

        // ── API: Get ALL teachers by semester + subject + professionalClass ──
        [HttpGet]
        public async Task<IActionResult> GetTeachersBySubject(
            string semester, string subject, string? professionalClass)
        {
            var query = _context.Teachers
                .Where(t => t.IsActive &&
                            t.Semester == semester &&
                            t.Subject == subject);

            if (!string.IsNullOrEmpty(professionalClass))
                query = query.Where(t => t.ProfessionalClass == professionalClass);

            var teachers = await query
                .Select(t => new { t.Id, t.FullName, t.Designation })
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return Json(teachers);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(TeacherFeedback model)
        {
            if (!ModelState.IsValid)
            {
                var semesters = await _context.Teachers
                    .Where(t => t.IsActive)
                    .Select(t => t.Semester)
                    .Distinct().OrderBy(s => s).ToListAsync();
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
                ModelState.AddModelError("",
                    "Something went wrong. Please try again.");
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
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // ── Stats ──
            ViewBag.TotalCount = await _context.TeacherFeedbacks.CountAsync();
            ViewBag.TodayCount = await _context.TeacherFeedbacks
                .CountAsync(f => f.SubmittedDate.Date == today);
            ViewBag.WeekCount = await _context.TeacherFeedbacks
                .CountAsync(f => f.SubmittedDate.Date >= weekStart);
            ViewBag.MonthCount = await _context.TeacherFeedbacks
                .CountAsync(f => f.SubmittedDate.Date >= monthStart);
            ViewBag.PendingCount = await _context.TeacherFeedbacks
                .CountAsync(f => !f.IsReviewed);

            // ── Rating distribution ──
            var allFeedbacks = await _context.TeacherFeedbacks.ToListAsync();
            ViewBag.ExcellentCount = allFeedbacks
                .Count(f => f.Rating == "Excellent");
            ViewBag.GoodCount = allFeedbacks
                .Count(f => f.Rating == "Good");
            ViewBag.AverageCount = allFeedbacks
                .Count(f => f.Rating == "Average");
            ViewBag.BelowAvgCount = allFeedbacks
                .Count(f => f.Rating == "Below Average");

            // ── Teacher list for filter ──
            ViewBag.Teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .OrderBy(t => t.FullName)
                .ToListAsync();
            ViewBag.Semesters = await _context.TeacherFeedbacks
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
                    Total = _context.TeacherFeedbacks
                                    .Count(f => f.TeacherId == t.Id),
                    Excellent = _context.TeacherFeedbacks
                                    .Count(f => f.TeacherId == t.Id &&
                                                f.Rating == "Excellent"),
                    Good = _context.TeacherFeedbacks
                                    .Count(f => f.TeacherId == t.Id &&
                                                f.Rating == "Good"),
                    Average = _context.TeacherFeedbacks
                                    .Count(f => f.TeacherId == t.Id &&
                                                f.Rating == "Average"),
                    BelowAvg = _context.TeacherFeedbacks
                                    .Count(f => f.TeacherId == t.Id &&
                                                f.Rating == "Below Average")
                })
                .ToListAsync();

            ViewBag.TeacherSummary = teacherSummary;

            // ── Apply filters ──
            var query = _context.TeacherFeedbacks
                .Include(f => f.Teacher)
                .AsQueryable();

            if (!string.IsNullOrEmpty(teacherFilter) &&
                int.TryParse(teacherFilter, out int tid))
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
            ViewBag.TotalPages = (int)Math.Ceiling(
                                         totalRecords / (double)pageSize);
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

        // ── Add Teacher ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(Teacher model)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill all required fields.";
                return RedirectToAction("ManageTeachers");
            }

            model.CreatedAt = DateTime.Now;
            model.IsActive = true;
            _context.Teachers.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Teacher {model.FullName} added successfully!";
            return RedirectToAction("ManageTeachers");
        }

        // ── Delete Teacher ──
        [HttpPost]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
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
        // ✅ CORRECT — returning Teacher list
        [AllowAnonymous]
        public async Task<IActionResult> ManageTeachers()
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var teachers = await _context.Teachers  // ✅ correct DbSet
                .Where(t => t.IsActive)
                .OrderBy(t => t.Semester)
                .ThenBy(t => t.FullName)
                .ToListAsync();

            return View(teachers);
        }

        // ── Dedicated Teachers List Route ──
        // ── Teachers List page ──
        [AllowAnonymous]
        public async Task<IActionResult> TeachersList()
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            // ✅ FIXED — returns Teacher list not TeacherFeedback list
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

            return View(teachers); // ✅ returns List<Teacher>
        }

        // ── Delete single feedback ──
        [HttpPost]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
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
        public async Task<IActionResult> BulkDeleteFeedback(
            [FromBody] List<int> ids)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
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
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("FeedbackAdmin")))
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
            csv.AppendLine("ID,Teacher,Subject,Semester,Student,LCID," +
                           "Rating,Feedback,Anonymous,Date,Reviewed");

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