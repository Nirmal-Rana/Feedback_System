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
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedPhotoExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxPhotoBytes = 3 * 1024 * 1024; // 3 MB

        public FeedbackController(
            ApplicationDbContext context,
            ILogger<FeedbackController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        // ═══════════════════════════════════════
        // Photo upload helpers
        // ═══════════════════════════════════════

        private async Task<(bool Success, string? RelativePath, string? Error)> SaveTeacherPhotoAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedPhotoExtensions.Contains(ext))
                return (false, null, "Photo must be a JPG, PNG, or WEBP file.");

            if (file.Length > MaxPhotoBytes)
                return (false, null, "Photo must be smaller than 3 MB.");

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "teachers");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return (true, $"/uploads/teachers/{fileName}", null);
        }

        private void DeleteTeacherPhoto(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var fullPath = Path.Combine(
                _env.WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(fullPath))
            {
                try { System.IO.File.Delete(fullPath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not delete old teacher photo: {Path}", fullPath); }
            }
        }

        // ═══════════════════════════════════════
        // Comma-list helper (used for both Semester and Section fields)
        // ═══════════════════════════════════════

        private static List<string> SplitSemesters(string? raw) =>
            (raw ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

        private static bool TeacherMatchesSemester(Teacher t, string semester)
        {
            var tokens = SplitSemesters(t.Semester);
            return tokens.Contains(semester) || tokens.Contains("ALL");
        }

        // ═══════════════════════════════════════
        // STUDENT — Submit Feedback Form
        // ═══════════════════════════════════════

        [HttpGet]
        public async Task<IActionResult> SubmitFeedback()
        {
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
            var teachers = await _context.Teachers
                .Where(t => t.IsActive && (t.Semester.Contains(semester) || t.Semester.Contains("ALL")))
                .ToListAsync();

            // Flatten multi-assigned sections so students see individual clean section options
            var subjects = teachers
                .SelectMany(t => {
                    var sections = string.IsNullOrEmpty(t.Section)
                        ? new[] { "" }
                        : t.Section.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim());

                    return sections.Select(sec => new { t.Subject, Section = sec });
                })
                .Distinct()
                .OrderBy(t => t.Subject)
                .ToList();

            return Json(subjects);
        }

        // ── API: Get ALL teachers by semester + subject + section ──
        [HttpGet]
        public async Task<IActionResult> GetTeachersBySubject(
            string semester, string subject, string? section)
        {
            var query = _context.Teachers.Where(t => t.IsActive && t.Subject == subject);
            var teachersList = await query.ToListAsync();

            var filtered = teachersList
                .Where(t =>
                    (t.Semester.Split(',').Select(s => s.Trim()).Contains(semester) || t.Semester.Contains("ALL")) &&
                    (string.IsNullOrEmpty(section) ||
                     (t.Section != null && t.Section.Split(',').Select(c => c.Trim()).Contains(section)))
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

            ViewBag.TotalCount = await activeFeedbacksQuery.CountAsync();
            ViewBag.TodayCount = await activeFeedbacksQuery.CountAsync(f => f.SubmittedDate.Date == today);
            ViewBag.WeekCount = await activeFeedbacksQuery.CountAsync(f => f.SubmittedDate.Date >= weekStart);
            ViewBag.MonthCount = await activeFeedbacksQuery.CountAsync(f => f.SubmittedDate.Date >= monthStart);
            ViewBag.PendingCount = await activeFeedbacksQuery.CountAsync(f => f.IsReviewed);

            var allFeedbacks = await activeFeedbacksQuery.ToListAsync();
            ViewBag.ExcellentCount = allFeedbacks.Count(f => f.Rating == "Excellent");
            ViewBag.GoodCount = allFeedbacks.Count(f => f.Rating == "Good");
            ViewBag.AverageCount = allFeedbacks.Count(f => f.Rating == "Average");
            ViewBag.BelowAvgCount = allFeedbacks.Count(f => f.Rating == "Below Average");

            ViewBag.Teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .OrderBy(t => t.FullName)
                .ToListAsync();

            ViewBag.Semesters = await activeFeedbacksQuery
                .Select(f => f.Semester).Distinct().ToListAsync();

            var teacherSummary = await _context.Teachers
                .Where(t => t.IsActive)
                .Select(t => new
                {
                    t.Id,
                    t.FullName,
                    t.Subject,
                    t.Semester,
                    t.PhotoPath,
                    Total = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id),
                    Excellent = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Excellent"),
                    Good = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Good"),
                    Average = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Average"),
                    BelowAvg = _context.TeacherFeedbacks.Count(f => f.TeacherId == t.Id && f.Rating == "Below Average")
                })
                .ToListAsync();

            ViewBag.TeacherSummary = teacherSummary;

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
        // `semester`: when present, only that semester's feedback is shown.
        // `section`: when present, further narrows feedback down to that one section.
        public async Task<IActionResult> TeacherDetail(int id, string? semester, string? section)
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            ViewBag.TeacherId = teacher.Id;
            ViewBag.TeacherName = teacher.FullName;
            ViewBag.Subject = teacher.Subject;
            ViewBag.FilterSemester = semester;
            ViewBag.FilterSection = section;
            ViewBag.TeacherPhoto = teacher.PhotoPath;

            ViewBag.AssignedSections = SplitSemesters(teacher.Section)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var feedbackQuery = _context.TeacherFeedbacks.Where(f => f.TeacherId == id);

            if (!string.IsNullOrWhiteSpace(semester))
            {
                feedbackQuery = feedbackQuery.Where(f => f.Semester == semester);
            }

            if (!string.IsNullOrWhiteSpace(section))
            {
                feedbackQuery = feedbackQuery.Where(f => f.Section == section);
            }

            var feedbacks = await feedbackQuery
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

        // ── Add Teacher (accepts arrays for Multi-Selection + optional photo) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(
            string FullName, string Subject, string[] Semesters, string[] Sections,
            string Designation, IFormFile? PhotoFile)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Subject))
            {
                TempData["Error"] = "Full Name and Subject are required fields.";
                return RedirectToAction("ManageTeachers");
            }

            if (Semesters == null || Semesters.Length == 0 || Sections == null || Sections.Length == 0)
            {
                TempData["Error"] = "Please select at least one Semester and one Section.";
                return RedirectToAction("ManageTeachers");
            }

            try
            {
                string? photoPath = null;
                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    var (success, relPath, error) = await SaveTeacherPhotoAsync(PhotoFile);
                    if (!success)
                    {
                        TempData["Error"] = error;
                        return RedirectToAction("ManageTeachers");
                    }
                    photoPath = relPath;
                }

                string combinedSemesters = string.Join(", ", Semesters);
                string combinedSections = string.Join(", ", Sections);

                var teacher = new Teacher
                {
                    FullName = FullName.Trim(),
                    Subject = Subject.Trim(),
                    Semester = combinedSemesters,
                    Section = combinedSections,
                    Designation = string.IsNullOrWhiteSpace(Designation) ? "Lecturer" : Designation.Trim(),
                    PhotoPath = photoPath,
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

        // ── Edit Teacher (mirrors AddTeacher; updates an existing record in place) ──
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(
            int Id, string FullName, string Subject, string[] Semesters, string[] Sections,
            string Designation, IFormFile? PhotoFile, bool RemovePhoto = false)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Subject))
            {
                TempData["Error"] = "Full Name and Subject are required fields.";
                return RedirectToAction("ManageTeachers");
            }

            if (Semesters == null || Semesters.Length == 0 || Sections == null || Sections.Length == 0)
            {
                TempData["Error"] = "Please select at least one Semester and one Section.";
                return RedirectToAction("ManageTeachers");
            }

            try
            {
                var teacher = await _context.Teachers.FindAsync(Id);
                if (teacher == null || !teacher.IsActive)
                {
                    TempData["Error"] = "Teacher not found.";
                    return RedirectToAction("ManageTeachers");
                }

                teacher.FullName = FullName.Trim();
                teacher.Subject = Subject.Trim();
                teacher.Semester = string.Join(", ", Semesters);
                teacher.Section = string.Join(", ", Sections);
                teacher.Designation = string.IsNullOrWhiteSpace(Designation) ? "Lecturer" : Designation.Trim();

                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    var (success, relPath, error) = await SaveTeacherPhotoAsync(PhotoFile);
                    if (!success)
                    {
                        TempData["Error"] = error;
                        return RedirectToAction("ManageTeachers");
                    }

                    DeleteTeacherPhoto(teacher.PhotoPath);
                    teacher.PhotoPath = relPath;
                }
                else if (RemovePhoto)
                {
                    DeleteTeacherPhoto(teacher.PhotoPath);
                    teacher.PhotoPath = null;
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = $"Teacher {teacher.FullName} updated successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while executing EditTeacher");
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
                teacher.IsActive = false;
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "Teacher removed successfully.";
            return RedirectToAction("ManageTeachers");
        }

        // ── Bulk delete teachers ──
        [HttpPost]
        public async Task<IActionResult> BulkDeleteTeachers([FromBody] List<int> ids)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return Unauthorized();

            if (ids == null || !ids.Any()) return BadRequest();

            var teachers = await _context.Teachers
                .Where(t => ids.Contains(t.Id) && t.IsActive)
                .ToListAsync();

            foreach (var teacher in teachers)
            {
                teacher.IsActive = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new { deleted = teachers.Count });
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

            var feedbackCounts = await _context.TeacherFeedbacks
                .GroupBy(f => f.TeacherId)
                .Select(g => new { TeacherId = g.Key, Count = g.Count() })
                .ToListAsync();

            ViewBag.FeedbackCounts = feedbackCounts.ToDictionary(f => f.TeacherId, f => f.Count);

            return View(teachers);
        }

        // ═══════════════════════════════════════
        // Teachers List — grouped by semester
        // ═══════════════════════════════════════

        [AllowAnonymous]
        public async Task<IActionResult> TeachersList()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            var teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .ToListAsync();

            var allFeedbacks = await _context.TeacherFeedbacks.ToListAsync();

            var realSemesters = teachers
                .SelectMany(t => SplitSemesters(t.Semester))
                .Where(s => s != "ALL")
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            var summary = realSemesters
                .Select(sem =>
                {
                    var teacherIdsInSem = teachers
                        .Where(t => TeacherMatchesSemester(t, sem))
                        .Select(t => t.Id)
                        .ToList();

                    return new SemesterSummary
                    {
                        Semester = sem,
                        TeacherCount = teacherIdsInSem.Count,
                        FeedbackCount = allFeedbacks.Count(f => f.Semester == sem && teacherIdsInSem.Contains(f.TeacherId))
                    };
                })
                .ToList();

            return View(summary);
        }

        [AllowAnonymous]
        public async Task<IActionResult> TeachersBySemester(string semester)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("FeedbackAdmin")))
                return RedirectToAction("Login", "Admin");

            if (string.IsNullOrWhiteSpace(semester))
                return RedirectToAction("TeachersList");

            var teachers = await _context.Teachers
                .Where(t => t.IsActive)
                .ToListAsync();

            var filtered = teachers
                .Where(t => TeacherMatchesSemester(t, semester))
                .OrderBy(t => t.FullName)
                .ToList();

            var feedbackCounts = await _context.TeacherFeedbacks
                .Where(f => f.Semester == semester)
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
                countsDict[f.TeacherId] = new { Count = f.Count, Excellent = f.Excellent };
            }

            ViewBag.FeedbackCounts = countsDict;
            ViewBag.Semester = semester;

            return View(filtered);
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
            csv.AppendLine("ID,Teacher,Subject,Semester,Section,Student,LCID,Rating,Feedback,Anonymous,Date,Reviewed");

            foreach (var f in feedbacks)
            {
                csv.AppendLine(
                    $"{f.Id}," +
                    $"\"{f.Teacher?.FullName ?? ""}\"," +
                    $"\"{f.Subject}\"," +
                    $"\"{f.Semester}\"," +
                    $"\"{f.Section ?? ""}\"," +
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