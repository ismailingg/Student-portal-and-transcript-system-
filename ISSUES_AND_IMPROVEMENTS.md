# Issues and Improvements

This document outlines identified issues, security concerns, and potential improvements for the University Management System.

---

## 🔴 Critical Security Issues

### 1. **Hardcoded Database Credentials**
**Location:** `appsettings.json`
**Issue:** Database password is hardcoded in source control
```json
"Password=Mamma_mia101"
```
**Risk:** High - Credentials exposed in version control
**Fix:** 
- Use User Secrets for development: `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "..." `
- Use environment variables or Azure Key Vault for production
- Add `appsettings.json` to `.gitignore` if not already

### 2. **Account Lockout Disabled**
**Location:** `AccountController.cs` line 38
**Issue:** `lockoutOnFailure: false` disables brute-force protection
```csharp
await _signInManager.PasswordSignInAsync(..., lockoutOnFailure: false);
```
**Risk:** Medium - Vulnerable to brute-force attacks
**Fix:** Enable lockout and configure in `Program.cs`:
```csharp
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
```

### 3. **Missing HTTPS Enforcement**
**Location:** `Program.cs`
**Issue:** No explicit HTTPS redirection enforcement
**Risk:** Medium - Data transmitted in plain text
**Fix:** Ensure HTTPS is enforced in production:
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
```

### 4. **No Password Reset Functionality**
**Location:** `AccountController.cs`
**Issue:** Users cannot reset forgotten passwords
**Risk:** Medium - Poor user experience and security
**Fix:** Implement password reset with email confirmation

---

## 🟡 Business Logic Issues

### 5. **Missing Credit Limit Validation**
**Location:** `StudentController.cs` - `Enroll` method
**Issue:** Student enrollment doesn't enforce 9-18 credit hour limits
**Current:** Only checks prerequisites and duplicate enrollment
**Expected:** Should validate total credits before allowing enrollment
**Fix:** Add credit calculation before enrollment:
```csharp
var currentSemesterCredits = await CalculateCurrentSemesterCredits(student.StudentId, offering.SemesterId);
var newCourseCredits = offering.Course.Credits;
if (currentSemesterCredits + newCourseCredits > 18)
{
    TempData["Error"] = "Cannot exceed 18 credit hours per semester.";
    return RedirectToAction(nameof(Register));
}
if (currentSemesterCredits + newCourseCredits < 9 && currentSemesterCredits == 0)
{
    TempData["Warning"] = "Minimum 9 credits required per semester.";
}
```

### 6. **No Course Capacity Check**
**Location:** `StudentController.cs` - `Enroll` method
**Issue:** Students can enroll even if course is full
**Fix:** Check `CourseOffering.Capacity` vs current enrollments:
```csharp
var currentEnrollments = await _context.Enrollments
    .CountAsync(e => e.CourseOfferingId == courseOfferingId);
if (currentEnrollments >= offering.Capacity)
{
    TempData["Error"] = "Course is full.";
    return RedirectToAction(nameof(Register));
}
```

### 7. **No Grade Range Validation**
**Location:** `FacultyController.cs` - `UpdateGrade` method
**Issue:** Grades can be set outside 0-100 range
**Fix:** Add validation:
```csharp
if (assign.HasValue && (assign < 0 || assign > 100))
    ModelState.AddModelError("assign", "Score must be between 0 and 100");
// Repeat for quiz, project, mid, final
```

### 8. **Missing Time Conflict Check**
**Location:** `StudentController.cs`
**Issue:** No validation for overlapping course schedules
**Fix:** Add schedule conflict detection (requires adding time/day fields to CourseOffering)

---

## 🟠 Code Quality Issues

### 9. **Debug Code in Production**
**Location:** Multiple controllers
**Issue:** `System.Diagnostics.Debug.WriteLine()` statements left in code
- `CoursesController.cs` line 59
- `CourseOfferingsController.cs` line 60
**Fix:** Replace with proper logging:
```csharp
private readonly ILogger<CoursesController> _logger;
// Then use: _logger.LogError("Error message");
```

### 10. **No Proper Logging Infrastructure**
**Location:** All controllers
**Issue:** No `ILogger` injection, using `Debug.WriteLine` instead
**Fix:** 
- Inject `ILogger<T>` in all controllers
- Use structured logging
- Configure logging levels in `appsettings.json`

### 11. **Missing Error Handling**
**Location:** Multiple controllers
**Issue:** No try-catch blocks for database operations
**Risk:** Unhandled exceptions expose stack traces
**Fix:** Add exception handling:
```csharp
try
{
    await _context.SaveChangesAsync();
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "Error saving changes");
    ModelState.AddModelError("", "An error occurred. Please try again.");
    return View(model);
}
```

### 12. **No Transaction Management**
**Location:** `AdminController.cs` - `CreateStudent` and `CreateFaculty`
**Issue:** Multi-step operations (create user + create profile) not wrapped in transaction
**Risk:** Partial data if second step fails
**Fix:** Use transactions:
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Create user
    // Create profile
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 13. **Code Duplication**
**Location:** Multiple files
**Issue:** `GradeToPoint` method duplicated in `HomeController` and `FacultyController`
**Fix:** Extract to a shared service or utility class:
```csharp
public static class GradeHelper
{
    public static double GradeToPoint(string? grade) { ... }
    public static string CalculateLetterGrade(double total) { ... }
}
```

### 14. **Business Logic in Controllers**
**Location:** All controllers
**Issue:** Complex business logic mixed with HTTP handling
**Fix:** Extract to service layer:
- Create `Services/` folder
- `IEnrollmentService`, `IGradingService`, `IAttendanceService`
- Move business logic from controllers to services

---

## 🔵 Missing Features

### 15. **No Password Change Functionality**
**Location:** `AccountController.cs`
**Issue:** Users cannot change their passwords
**Fix:** Add `ChangePassword` action

### 16. **No Email Notifications**
**Location:** Entire application
**Issue:** No email notifications for:
- Course enrollment confirmations
- Grade submissions
- Attendance warnings
**Fix:** Integrate email service (SendGrid, SMTP)

### 17. **No Audit Logging**
**Location:** Entire application
**Issue:** No tracking of who changed what and when
**Fix:** Implement audit trail:
- Add `AuditLog` entity
- Track all CRUD operations
- Log user actions with timestamps

### 18. **No Input Sanitization**
**Location:** All controllers accepting user input
**Issue:** Potential XSS vulnerabilities
**Fix:** 
- Use Razor's built-in HTML encoding
- Validate and sanitize all inputs
- Consider using HTML sanitization library

### 19. **No Rate Limiting**
**Location:** All endpoints
**Issue:** Vulnerable to DoS attacks
**Fix:** Implement rate limiting middleware:
```csharp
builder.Services.AddRateLimiter(options => { ... });
```

### 20. **No API Documentation**
**Location:** Entire application
**Issue:** No Swagger/OpenAPI documentation
**Fix:** Add Swagger/OpenAPI:
```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

---

## 🟢 Architecture Improvements

### 21. **Repository Pattern**
**Current:** Direct `DbContext` access in controllers
**Improvement:** Implement repository pattern for data access abstraction

### 22. **Unit of Work Pattern**
**Current:** Multiple `SaveChangesAsync()` calls
**Improvement:** Implement UoW pattern for better transaction management

### 23. **Dependency Injection for Services**
**Current:** Business logic in controllers
**Improvement:** Create service interfaces and register in DI container

### 24. **AutoMapper for DTOs**
**Current:** Manual mapping between models and view models
**Improvement:** Use AutoMapper for cleaner code

### 25. **Caching Strategy**
**Current:** No caching
**Improvement:** Cache frequently accessed data (departments, courses list)

---

## 📋 Testing Improvements

### 26. **No Unit Tests**
**Issue:** No test coverage
**Fix:** Add xUnit or NUnit tests for:
- Business logic services
- Controller actions
- Model validation

### 27. **No Integration Tests**
**Issue:** No end-to-end testing
**Fix:** Add integration tests for:
- Database operations
- Authentication flows
- Enrollment workflows

---

## 🔧 Configuration Improvements

### 28. **Hardcoded Values**
**Location:** Multiple files
**Issue:** Magic numbers and strings (e.g., "Admin@123", 75% attendance threshold)
**Fix:** Move to `appsettings.json`:
```json
{
  "BusinessRules": {
    "MinCreditsPerSemester": 9,
    "MaxCreditsPerSemester": 18,
    "MinAttendancePercentage": 75
  }
}
```

### 29. **No Environment-Specific Configurations**
**Issue:** Single `appsettings.json` for all environments
**Fix:** Use `appsettings.Development.json` and `appsettings.Production.json`

---

## 📊 Performance Improvements

### 30. **N+1 Query Problem**
**Location:** `StudentController.cs` - `Index` method
**Issue:** Multiple queries in loop (line 52-54)
**Fix:** Use eager loading or batch queries:
```csharp
var attendanceRecords = await _context.Attendances
    .Where(a => a.StudentId == student.StudentId && 
                myEnrollments.Select(e => e.CourseOfferingId).Contains(a.CourseOfferingId))
    .GroupBy(a => a.CourseOfferingId)
    .ToDictionaryAsync(g => g.Key, g => CalculatePercentage(g));
```

### 31. **Missing Database Indexes**
**Location:** `UniversityDbContext.cs`
**Issue:** No explicit indexes on frequently queried fields
**Fix:** Add indexes:
```csharp
modelBuilder.Entity<Enrollment>()
    .HasIndex(e => new { e.StudentId, e.CourseOfferingId });
```

---

## 🎯 Priority Recommendations

### High Priority (Security & Critical Bugs)
1. Fix hardcoded credentials (#1)
2. Enable account lockout (#2)
3. Add credit limit validation (#5)
4. Add course capacity check (#6)
5. Remove debug code (#9)

### Medium Priority (Code Quality)
6. Implement proper logging (#10)
7. Add error handling (#11)
8. Extract business logic to services (#14)
9. Add transaction management (#12)

### Low Priority (Enhancements)
10. Add password reset (#4, #15)
11. Implement email notifications (#16)
12. Add unit tests (#26)
13. Implement repository pattern (#21)

---

## 📝 Notes

- Review and prioritize based on your project requirements
- Some improvements may require significant refactoring
- Consider creating GitHub issues for tracking
- Test all changes thoroughly before deployment

---

**Last Updated:** Generated from codebase analysis
