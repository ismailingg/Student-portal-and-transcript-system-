using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WP_project.Models;
using WP_project.Data;

namespace WP_project.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly UniversityDbContext _context;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, UniversityDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return View();
            }

            var dashboardData = new DashboardViewModel
            {
                UserName = user.FullName ?? user.UserName ?? "User",
                Email = user.Email ?? "",
                Role = user.Role
            };

            if (User.IsInRole("Student") && user.StudentId.HasValue)
            {
                var student = await _context.Students
                    .Include(s => s.Department)
                    // Remove .Include(s => s.Transcript) - we calculate manually
                    .Include(s => s.Enrollments)
                        .ThenInclude(e => e.CourseOffering)
                            .ThenInclude(co => co.Course)
                    .FirstOrDefaultAsync(s => s.StudentId == user.StudentId.Value);

                if (student != null)
                {
                    dashboardData.StudentName = student.Name;
                    dashboardData.RollNumber = student.RollNumber;
                    dashboardData.DepartmentName = student.Department?.Name ?? "N/A";
                    
                    // Calculate CGPA Live
                    double totalPoints = 0;
                    double totalCredits = 0;
                    int creditsInProgress = 0;
                    
                    foreach (var enrollment in student.Enrollments)
                    {
                        if (enrollment.Grade != null)
                        {
                            double points = GradeToPoint(enrollment.Grade);
                            int credits = enrollment.CourseOffering.Course.Credits;
                            
                            // If F, we count attempted credits but 0 points
                            totalPoints += (points * credits);
                            totalCredits += credits;
                        }
                        else
                        {
                            creditsInProgress += enrollment.CourseOffering.Course.Credits;
                        }
                    }

                    // Apply Rule: Minimum 9 Credits to show CGPA
                    if (totalCredits < 9)
                    {
                        dashboardData.CGPA = 0;
                    }
                    else
                    {
                        dashboardData.CGPA = totalCredits > 0 ? (decimal)(totalPoints / totalCredits) : 0;
                    }

                    dashboardData.TotalCredits = (int)totalCredits;
                    dashboardData.CreditsInProgress = creditsInProgress;
                    dashboardData.EnrolledCourses = student.Enrollments.Count;
                }
            }
            else if (User.IsInRole("Faculty") && user.FacultyId.HasValue)
            {
                var faculty = await _context.Faculties
                    .Include(f => f.Department)
                    .Include(f => f.CourseOfferings)
                        .ThenInclude(co => co.Course)
                    .FirstOrDefaultAsync(f => f.FacultyId == user.FacultyId.Value);

                if (faculty != null)
                {
                    dashboardData.FacultyName = faculty.Name;
                    dashboardData.DepartmentName = faculty.Department?.Name ?? "N/A";
                    dashboardData.TotalClasses = faculty.CourseOfferings.Count;
                }
            }
            else if (User.IsInRole("Admin"))
            {
                dashboardData.TotalUsers = await _context.Users.CountAsync();
                dashboardData.TotalDepartments = await _context.Departments.CountAsync();
                dashboardData.TotalCourses = await _context.Courses.CountAsync();
            }

            return View(dashboardData);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
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
    }

    public class DashboardViewModel
    {
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        
        // Student specific
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public decimal CGPA { get; set; }
        public int TotalCredits { get; set; }
        public int CreditsInProgress { get; set; } // New Field
        public int EnrolledCourses { get; set; }
        
        // Faculty specific
        public string? FacultyName { get; set; }
        public int TotalClasses { get; set; }
        
        // Common
        public string? DepartmentName { get; set; }
        
        // Admin specific
        public int TotalUsers { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalCourses { get; set; }
    }
}
