using CollegeIssueManagement.Data;
using CollegeIssueManagement.Models;
using Microsoft.AspNetCore.Authorization;   
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CollegeIssueManagement.Controllers
{
    [AllowAnonymous]   
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AttendanceController> _logger;

        public AttendanceController(
            ApplicationDbContext context,
            ILogger<AttendanceController> logger)
        {
            _context = context;
            _logger = logger;
        }

       

        [HttpGet]
        public IActionResult SubmitAbsence()
        {
            return View(new AbsenceRecord());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAbsence(AbsenceRecord model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.SubmittedDate = DateTime.Now;
                _context.AbsenceRecords.Add(model);
                await _context.SaveChangesAsync();

                TempData["AbsenceSuccess"] = "true";
                TempData["AbsenceName"] = model.StudentName;
                TempData["AbsenceId"] = model.Id.ToString();

                return RedirectToAction("AbsenceConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving absence record");
                ModelState.AddModelError("",
                    "Something went wrong. Please try again.");
                return View(model);
            }
        }

        public IActionResult AbsenceConfirmation()
        {
            if (TempData["AbsenceSuccess"] == null)
                return RedirectToAction("SubmitAbsence");
            return View();
        }

       

        [HttpGet]
        public IActionResult AttendanceLogin()
        {
            if (HttpContext.Session.GetString("AttendanceAdmin") != null)
                return RedirectToAction("AttendanceDashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AttendanceLogin(string username, string password)
        {
            
            username = (username ?? "").Trim();
            password = (password ?? "").Trim();

            if (username == "attendance" && password == "Texas@2026")
            {
                HttpContext.Session.SetString("AttendanceAdmin", "true");
                return RedirectToAction("AttendanceDashboard");
            }

            ViewBag.Error = "Invalid username or password.";
            return View();
        }

       
        public IActionResult AttendanceLogout()
        {
            HttpContext.Session.Remove("AttendanceAdmin");
            return RedirectToAction("Index", "Home");
        }



        public async Task<IActionResult> AttendanceDashboard(
            string? dateFilter,
            string? semesterFilter,
            string? sectionFilter,
            string? statusFilter,
            int page = 1)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("AttendanceAdmin")))
                return RedirectToAction("AttendanceLogin");

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            ViewBag.TodayCount = await _context.AbsenceRecords
                .CountAsync(r => r.SubmittedDate.Date == today);
            ViewBag.WeekCount = await _context.AbsenceRecords
                .CountAsync(r => r.SubmittedDate.Date >= weekStart);
            ViewBag.MonthCount = await _context.AbsenceRecords
                .CountAsync(r => r.SubmittedDate.Date >= monthStart);
            ViewBag.TotalCount = await _context.AbsenceRecords.CountAsync();
            ViewBag.PendingCount = await _context.AbsenceRecords
                .CountAsync(r => !r.IsReviewed);

            ViewBag.Semesters = await _context.AbsenceRecords
                .Select(r => r.Semester).Distinct().ToListAsync();
            ViewBag.Sections = await _context.AbsenceRecords
                .Select(r => r.Section).Distinct().ToListAsync();

            var query = _context.AbsenceRecords.AsQueryable();

            if (!string.IsNullOrEmpty(dateFilter) &&
                DateTime.TryParse(dateFilter, out DateTime fd))
                query = query.Where(r => r.SubmittedDate.Date == fd.Date);

            if (!string.IsNullOrEmpty(semesterFilter))
                query = query.Where(r => r.Semester == semesterFilter);

            if (!string.IsNullOrEmpty(sectionFilter))
                query = query.Where(r => r.Section == sectionFilter);

            if (statusFilter == "pending")
                query = query.Where(r => !r.IsReviewed);
            else if (statusFilter == "reviewed")
                query = query.Where(r => r.IsReviewed);

            int pageSize = 15;
            int totalRecords = await query.CountAsync();
            var records = await query
                .OrderByDescending(r => r.SubmittedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(
                                        totalRecords / (double)pageSize);
            ViewBag.TotalFiltered = totalRecords;
            ViewBag.DateFilter = dateFilter;
            ViewBag.SemesterFilter = semesterFilter;
            ViewBag.SectionFilter = sectionFilter;
            ViewBag.StatusFilter = statusFilter;

            return View(records);
        }

        public async Task<IActionResult> ViewAbsence(int id)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("AttendanceAdmin")))
                return RedirectToAction("AttendanceLogin");

            var record = await _context.AbsenceRecords.FindAsync(id);
            if (record == null) return NotFound();

            if (!record.IsReviewed)
            {
                record.IsReviewed = true;
                record.ReviewedDate = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return View(record);
        }

        [HttpPost]
        public async Task<IActionResult> BulkDelete([FromBody] List<int> ids)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("AttendanceAdmin")))
                return Unauthorized();

            if (ids == null || !ids.Any())
                return BadRequest();

            var records = await _context.AbsenceRecords
                .Where(r => ids.Contains(r.Id))
                .ToListAsync();

            _context.AbsenceRecords.RemoveRange(records);
            await _context.SaveChangesAsync();

            return Ok(new { deleted = records.Count });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAbsence(int id)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("AttendanceAdmin")))
                return RedirectToAction("AttendanceLogin");

            var record = await _context.AbsenceRecords.FindAsync(id);
            if (record != null)
            {
                _context.AbsenceRecords.Remove(record);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("AttendanceDashboard");
        }

        public async Task<IActionResult> ExportExcel(
            string? startDate, string? endDate,
            string? semester, string? section)
        {
            if (string.IsNullOrEmpty(
                HttpContext.Session.GetString("AttendanceAdmin")))
                return RedirectToAction("AttendanceLogin");

            var query = _context.AbsenceRecords.AsQueryable();

            if (DateTime.TryParse(startDate, out DateTime s))
                query = query.Where(r => r.SubmittedDate.Date >= s.Date);
            if (DateTime.TryParse(endDate, out DateTime e))
                query = query.Where(r => r.SubmittedDate.Date <= e.Date);
            if (!string.IsNullOrEmpty(semester))
                query = query.Where(r => r.Semester == semester);
            if (!string.IsNullOrEmpty(section))
                query = query.Where(r => r.Section == section);

            var records = await query
                .OrderByDescending(r => r.SubmittedDate)
                .ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Student Name,LCID,Semester,Section," +
                           "Missed Class,Reason,Submitted Date,Reviewed");

            foreach (var r in records)
            {
                csv.AppendLine(
                    $"{r.Id}," +
                    $"\"{r.StudentName}\"," +
                    $"\"{r.LCID}\"," +
                    $"\"{r.Semester}\"," +
                    $"\"{r.Section}\"," +
                    $"\"{r.MissedClass}\"," +
                    $"\"{r.Reason.Replace("\"", "'")}\"," +
                    $"{r.SubmittedDate:dd/MM/yyyy HH:mm}," +
                    $"{(r.IsReviewed ? "Yes" : "No")}");
            }

            return File(
                Encoding.UTF8.GetBytes("\uFEFF" + csv.ToString()),
                "text/csv",
                $"Absences_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}