# University Course Registration & Transcript Management System

## 1. Project Overview
The **University Management System** is a full-stack web application built with **ASP.NET Core 8 MVC** and **PostgreSQL**. It serves as a centralized portal for:
*   **Admins:** To manage the academic structure (Departments, Courses, Semesters) and users.
*   **Faculty:** To manage assigned classes, record attendance, and submit detailed grades.
*   **Students:** To register for courses, view schedules, track attendance, and generate official transcripts.

The system enforces strict business rules such as prerequisite validation, credit hour limits (9-18 credits), and attendance requirements (minimum 75%).

---

## 2. Database Schema & Relationships

The database is normalized and handles complex academic relationships.

### **Core Tables**

#### 1. `AspNetUsers` (Identity)
*   **Role:** Stores login credentials and system access roles (`Admin`, `Faculty`, `Student`).
*   **Relationships:**
    *   One-to-One with `Students` table.
    *   One-to-One with `Faculties` table.

#### 2. `Departments`
*   **Description:** Academic departments (e.g., Computer Science).
*   **Columns:** `DepartmentId`, `Name`, `Code`.
*   **Relationships:**
    *   One-to-Many with `Courses`.
    *   One-to-Many with `Students`.
    *   One-to-Many with `Faculties`.

#### 3. `Courses`
*   **Description:** Catalog of all courses available (e.g., CS101).
*   **Columns:** `CourseId`, `Code`, `Title`, `Credits`, `DepartmentId`.
*   **Relationships:**
    *   One-to-Many with `CourseOfferings`.
    *   Self-Referencing Many-to-Many via `Prerequisites` table.

#### 4. `Semesters`
*   **Description:** Academic terms (e.g., Fall 2025).
*   **Columns:** `SemesterId`, `Name`, `StartDate`, `EndDate`.

#### 5. `CourseOfferings`
*   **Description:** A specific instance of a course taught in a specific semester.
*   **Columns:** `CourseOfferingId`, `Section`, `Capacity`, `SemesterId`, `CourseId`, `InstructorId`.
*   **Relationships:**
    *   Links `Course`, `Semester`, and `Faculty`.
    *   One-to-Many with `Enrollments`.

#### 6. `Enrollments`
*   **Description:** The link between a Student and a Course Offering (Registration).
*   **Columns:** `StudentId`, `CourseOfferingId`, `EnrollmentDate`.
*   **Grades:** Stores detailed breakdown (`AssignmentScore`, `QuizScore`, `ProjectScore`, `MidScore`, `FinalScore`, `TotalScore`, `Grade`).
*   **Relationships:**
    *   Composite Primary Key (`StudentId`, `CourseOfferingId`).

#### 7. `Attendances`
*   **Description:** Daily attendance records.
*   **Columns:** `AttendanceId`, `Date`, `IsPresent`, `StudentId`, `CourseOfferingId`.

#### 8. `Prerequisites`
*   **Description:** Rules defining course dependencies.
*   **Columns:** `CourseId` (The course), `RequiredCourseId` (The prerequisite).

#### 9. `Transcripts`
*   **Description:** Stores the calculated academic performance.
*   **Columns:** `TranscriptId`, `StudentId`, `CGPA`, `TotalCredits`.

---

## 3. Key Features & Business Logic

### **A. Student Module**
*   **Registration Validation:**
    *   **Prerequisites:** Cannot register if prerequisite courses are not passed.
    *   **Clash Check:** Cannot register for the same course twice in one semester.
    *   **Credit Limits:** 
        *   **Max:** Cannot exceed 18 credit hours per semester.
        *   **Min:** Transcript is hidden if total credits < 9.
*   **Dashboard:** Live view of CGPA, Enrolled Courses count, and Credits in Progress.
*   **Transcript:** Generates a PDF-ready transcript with semester-wise breakdown.

### **B. Faculty Module**
*   **Grading System:**
    *   Weighted grading: Assignment (10%), Quiz (10%), Project (10%), Mid (20%), Final (50%).
    *   Auto-calculation of Letter Grades (A, B, C, F).
*   **Attendance System:**
    *   Faculty marks daily attendance.
    *   **Auto-Fail Rule:** If attendance < 75%, the system automatically assigns an 'F' grade regardless of scores.

### **C. Admin Module**
*   **User Management:** Create Faculty and Student accounts linked to Identity.
*   **Course Scheduling:** Assign courses to instructors and semesters.
*   **Data Management:** CRUD operations for Departments, Courses, and Semesters.

---

## 4. Technology Stack
*   **Backend:** .NET 8.0  (ASP.NET Core MVC)
*   **Database:** PostgreSQL (Entity Framework Core)
*   **Frontend:** Razor Views, Bootstrap 5, JavaScript
*   **Authentication:** ASP.NET Core Identity

