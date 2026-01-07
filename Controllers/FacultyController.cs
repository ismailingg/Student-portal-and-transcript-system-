using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WP_project.Data;
using WP_project.Models;

namespace WP_project.Controllers
{
    [Authorize(Roles = "Faculty")]
    public class FacultyController : Controller
    {
        private readonly UniversityDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FacultyController(UniversityDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<Faculty?> GetCurrentFacultyAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return null;

            return await _context.Faculties
                .FirstOrDefaultAsync(f => f.FacultyId == user.FacultyId);
        }

        // 1. My Courses
        public async Task<IActionResult> Index()
        {
            var faculty = await GetCurrentFacultyAsync();
            if (faculty == null) return RedirectToAction("Login", "Account");

            var myCourses = await _context.CourseOfferings
                .Include(c => c.Course)
                .Include(c => c.Semester)
                .Include(c => c.Enrollments)
                .Where(c => c.InstructorId == faculty.FacultyId)
                .OrderByDescending(c => c.Semester.StartDate)
                .ToListAsync();

            return View(myCourses);
        }

        // 2. Manage Grades (Detailed View)
        public async Task<IActionResult> ManageGrades(int id)
        {
            var faculty = await GetCurrentFacultyAsync();
            if (faculty == null) return Unauthorized();

            var offering = await _context.CourseOfferings
                .Include(c => c.Course)
                .Include(c => c.Semester)
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.CourseOfferingId == id);

            if (offering == null || offering.InstructorId != faculty.FacultyId)
            {
                return Unauthorized("You are not the instructor for this course.");
            }

            return View(offering);
        }

        // 3. Submit Detailed Grade
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateGrade(int enrollmentStudentId, int enrollmentCourseOfferingId, 
            double? assign, double? quiz, double? project, double? mid, double? final)
        {
            var faculty = await GetCurrentFacultyAsync();
            
            var enrollment = await _context.Enrollments
                .Include(e => e.CourseOffering)
                .FirstOrDefaultAsync(e => e.StudentId == enrollmentStudentId && e.CourseOfferingId == enrollmentCourseOfferingId);

            if (enrollment == null) return NotFound();

            if (enrollment.CourseOffering.InstructorId != faculty?.FacultyId)
            {
                return Unauthorized();
            }

            // Validate grade ranges (0-100)
            if (assign.HasValue && (assign < 0 || assign > 100))
            {
                ModelState.AddModelError("assign", "Assignment score must be between 0 and 100.");
            }
            if (quiz.HasValue && (quiz < 0 || quiz > 100))
            {
                ModelState.AddModelError("quiz", "Quiz score must be between 0 and 100.");
            }
            if (project.HasValue && (project < 0 || project > 100))
            {
                ModelState.AddModelError("project", "Project score must be between 0 and 100.");
            }
            if (mid.HasValue && (mid < 0 || mid > 100))
            {
                ModelState.AddModelError("mid", "Mid-term score must be between 0 and 100.");
            }
            if (final.HasValue && (final < 0 || final > 100))
            {
                ModelState.AddModelError("final", "Final exam score must be between 0 and 100.");
            }

            // If validation fails, return to ManageGrades view with errors
            if (!ModelState.IsValid)
            {
                var offering = await _context.CourseOfferings
                    .Include(c => c.Course)
                    .Include(c => c.Semester)
                    .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                    .FirstOrDefaultAsync(c => c.CourseOfferingId == enrollmentCourseOfferingId);

                if (offering == null) return NotFound();

                return View("ManageGrades", offering);
            }

            // Update Components
            enrollment.AssignmentScore = assign;
            enrollment.QuizScore = quiz;
            enrollment.ProjectScore = project;
            enrollment.MidScore = mid;
            enrollment.FinalScore = final;

            // Calculate Total Weighted Score
            double total = 0;
            total += (assign ?? 0) * 0.10;
            total += (quiz ?? 0) * 0.10;
            total += (project ?? 0) * 0.10;
            total += (mid ?? 0) * 0.20;
            total += (final ?? 0) * 0.50;

            enrollment.TotalScore = Math.Round(total, 1);
            // Check Attendance Rule ( < 75% = Automatic F)
            // Fetch total attendance stats
            var attendanceRecords = await _context.Attendances
                .Where(a => a.StudentId == enrollmentStudentId && a.CourseOfferingId == enrollmentCourseOfferingId)
                .ToListAsync();

            int totalClasses = attendanceRecords.Count;
            int presentCount = attendanceRecords.Count(a => a.IsPresent);
            double attendancePct = totalClasses > 0 ? (double)presentCount / totalClasses * 100 : 100; // Default to 100 if no records yet

            if (attendancePct < 75 && totalClasses > 0)
            {
                enrollment.Grade = "F";
                // enrollment.TotalScore = 0; // Optional: Reset score to 0? Or keep actual score but F grade?
                // Keeping score visible but forcing grade F is often better for transparency.
            }
            else
            {
                enrollment.Grade = CalculateLetterGrade(enrollment.TotalScore.Value);
            }

            await _context.SaveChangesAsync();

            // Trigger GPA Recalculation and Save to DB
            await RecalculateAndSaveCGPA(enrollmentStudentId);
            
            return RedirectToAction(nameof(ManageGrades), new { id = enrollmentCourseOfferingId });
        }

        private async Task RecalculateAndSaveCGPA(int studentId)
        {
            var enrollments = await _context.Enrollments
                .Include(e => e.CourseOffering)
                .ThenInclude(c => c.Course)
                .Where(e => e.StudentId == studentId && e.Grade != null)
                .ToListAsync();

            if (!enrollments.Any()) return;

            double totalPoints = 0;
            double totalCredits = 0;

            foreach (var e in enrollments)
            {
                double points = GradeToPoint(e.Grade);
                int credits = e.CourseOffering.Course.Credits;
                
                totalPoints += (points * credits);
                totalCredits += credits;
            }

            decimal cgpa = totalCredits > 0 ? (decimal)(totalPoints / totalCredits) : 0;

            var transcript = await _context.Transcripts.FirstOrDefaultAsync(t => t.StudentId == studentId);
            
            if (transcript == null)
            {
                transcript = new Transcript { StudentId = studentId };
                _context.Transcripts.Add(transcript);
            }

            transcript.CGPA = Math.Round(cgpa, 2);
            transcript.TotalCredits = (int)totalCredits;

            await _context.SaveChangesAsync();
        }

        private double GradeToPoint(string? grade)
        {
            return grade switch
            {
                "A" => 4.0,
                "A-" => 3.7,
                "B+" => 3.3,
                "B" => 3.0,
                "B-" => 2.7,
                "C+" => 2.3,
                "C" => 2.0,
                "D" => 1.0,
                "F" => 0.0,
                _ => 0.0
            };
        }

        private string CalculateLetterGrade(double total)
        {
            if (total >= 90) return "A";
            if (total >= 85) return "A-";
            if (total >= 80) return "B+";
            if (total >= 75) return "B";
            if (total >= 70) return "B-";
            if (total >= 65) return "C+";
            if (total >= 60) return "C";
            if (total >= 55) return "D";
            return "F";
        }

        // 4. Take Attendance UI
        public async Task<IActionResult> Attendance(int id)
        {
            var faculty = await GetCurrentFacultyAsync();
            if (faculty == null) return Unauthorized();

            var offering = await _context.CourseOfferings
                .Include(c => c.Course)
                .Include(c => c.Semester)
                .Include(c => c.Enrollments)
                .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(c => c.CourseOfferingId == id);

            if (offering == null || offering.InstructorId != faculty.FacultyId)
            {
                return Unauthorized();
            }

            ViewBag.Date = DateTime.Today.ToString("yyyy-MM-dd");
            return View(offering);
        }

        // 5. Submit Attendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitAttendance(int courseOfferingId, DateTime date, Dictionary<int, bool> status)
        {
            var faculty = await GetCurrentFacultyAsync();
            var offering = await _context.CourseOfferings.FindAsync(courseOfferingId);
            
            if (offering == null || offering.InstructorId != faculty?.FacultyId) 
                return Unauthorized();

            // Convert date to UTC for PostgreSQL
            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            foreach (var studentId in status.Keys)
            {
                bool isPresent = status[studentId];

                var existing = await _context.Attendances
                    .FirstOrDefaultAsync(a => a.CourseOfferingId == courseOfferingId && a.StudentId == studentId && a.Date == date);

                if (existing != null)
                {
                    existing.IsPresent = isPresent;
                }
                else
                {
                    _context.Attendances.Add(new Attendance
                    {
                        CourseOfferingId = courseOfferingId,
                        StudentId = studentId,
                        Date = date,
                        IsPresent = isPresent
                    });
                }
            }
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Attendance Saved Successfully";
            return RedirectToAction(nameof(Attendance), new { id = courseOfferingId });
        }
    }
}
