# University Course Registration & Transcript Management System

## Overview
A full-stack web application for managing university course registration, attendance tracking, and transcript generation. The system supports three user roles: **Admin**, **Faculty**, and **Student**, each with specific permissions and functionalities.

---

## .NET Version
- **.NET 8.0** (ASP.NET Core MVC)
- **Entity Framework Core 8.0.4**
- **ASP.NET Core Identity 8.0.4**
- **Npgsql.EntityFrameworkCore.PostgreSQL 8.0.4**

---

## Prerequisites
Before running this project, ensure you have the following installed:

1. **.NET 8.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **PostgreSQL** - [Download here](https://www.postgresql.org/download/)
3. **Visual Studio 2022** or **Visual Studio Code** (recommended for development)

---

## Project Structure

```
WP-Project/
├── Controllers/          # MVC Controllers (Account, Admin, Courses, Departments, etc.)
├── Models/              # Entity models (Student, Course, Enrollment, etc.)
├── Views/               # Razor view pages
├── Data/                # DbContext and database initialization
├── Migrations/          # Entity Framework migrations
├── ViewModels/          # View models for data transfer
├── wwwroot/            # Static files (CSS, JS, images)
├── Program.cs          # Application entry point and configuration
└── appsettings.json    # Configuration file (database connection, etc.)
```

---

## How It Works

### Architecture
The application follows the **Model-View-Controller (MVC)** pattern:

1. **Models**: Define the data structure (Student, Course, Department, etc.)
2. **Views**: Razor pages that render the UI
3. **Controllers**: Handle HTTP requests, process business logic, and return views

### Database
- Uses **PostgreSQL** as the database
- **Entity Framework Core** for ORM (Object-Relational Mapping)
- Database schema includes:
  - Users and Identity management
  - Departments, Courses, Semesters
  - Course Offerings and Enrollments
  - Attendance records
  - Transcripts and Grades
  - Prerequisites

### Authentication & Authorization
- **ASP.NET Core Identity** handles user authentication
- Role-based authorization:
  - **Admin**: Full system access
  - **Faculty**: Manage classes, attendance, and grades
  - **Student**: Register for courses and view transcripts

### Key Features

#### Student Module
- Course registration with validation (prerequisites, credit limits)
- View enrolled courses and schedules
- Track attendance
- Generate official transcripts
- Credit hour limits: 9-18 credits per semester

#### Faculty Module
- Manage assigned course offerings
- Record daily attendance
- Submit detailed grades (Assignments, Quizzes, Projects, Mid-term, Final)
- Automatic grade calculation with weighted percentages
- Auto-fail rule: Students with <75% attendance receive 'F' grade

#### Admin Module
- Manage departments, courses, and semesters
- Create and manage user accounts (Faculty and Students)
- Assign courses to instructors
- Full CRUD operations on academic data

---

## How to Use

### 1. Setup Database Connection

Edit `appsettings.json` and update the PostgreSQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=UniversityDB;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
  }
}
```

### 2. Create Database

Run the following commands in the terminal (from the project root directory):

```bash
# Restore packages
dotnet restore

# Apply database migrations
dotnet ef database update
```

### 3. Run the Application

```bash
# Run the application
dotnet run
```

Or use Visual Studio:
- Press `F5` or click the "Run" button

The application will start on `https://localhost:5001` or `http://localhost:5000` (check the console output for the exact URL).

### 4. Access the Application

- Open your browser and navigate to the URL shown in the console
- The database will be automatically seeded with initial data on first run
- Use the seeded admin credentials (check `DbInitializer.cs` for default accounts) to log in

---

## How to Edit

### Adding a New Controller

1. Create a new file in the `Controllers/` folder (e.g., `NewController.cs`)
2. Inherit from `Controller` base class
3. Add action methods (Index, Create, Edit, Delete, etc.)
4. Create corresponding views in `Views/NewController/`

Example:
```csharp
using Microsoft.AspNetCore.Mvc;

namespace WP_project.Controllers
{
    public class NewController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
```

### Adding a New Model

1. Create a new class in the `Models/` folder
2. Add properties and data annotations
3. Add the model to `UniversityDbContext.cs`:
   ```csharp
   public DbSet<YourModel> YourModels { get; set; }
   ```
4. Create and apply a migration:
   ```bash
   dotnet ef migrations add AddYourModel
   dotnet ef database update
   ```

### Modifying Views

- Views are located in the `Views/` folder
- Each controller has its own subfolder
- Shared views (layouts, partials) are in `Views/Shared/`
- Views use Razor syntax (`.cshtml` files)

### Changing Business Logic

- Business logic is primarily in **Controllers**
- For complex operations, consider creating service classes in a `Services/` folder
- Database operations use Entity Framework Core through `UniversityDbContext`

### Database Changes

When you modify models or add new entities:

1. Create a migration:
   ```bash
   dotnet ef migrations add YourMigrationName
   ```

2. Review the generated migration file in `Migrations/`

3. Apply changes to database:
   ```bash
   dotnet ef database update
   ```

### Configuration Changes

- **Database connection**: Edit `appsettings.json`
- **Identity settings**: Modify `Program.cs` (password requirements, etc.)
- **Application settings**: Add to `appsettings.json` and access via `IConfiguration`

---

## Development Tips

### Running Migrations
```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

### Debugging
- Set breakpoints in Controllers or Models
- Use `Console.WriteLine()` or logging for debugging
- Check browser developer tools (F12) for client-side issues

### Testing Changes
1. Make your changes
2. Build the project: `dotnet build`
3. Run the application: `dotnet run`
4. Test the functionality in the browser

---

## Common Tasks

### Adding a New User Role
1. Update `Program.cs` to seed the new role
2. Add role checks in controllers: `[Authorize(Roles = "NewRole")]`
3. Update `DbInitializer.cs` if needed

### Modifying Grade Calculation
- Edit the grading logic in `FacultyController.cs` or create a service class
- Update the `Enrollment` model if grade structure changes

### Changing UI Styling
- Modify `wwwroot/css/site.css` for global styles
- Edit individual view files for page-specific styles
- Bootstrap 5 is included for responsive design

---

## Troubleshooting

### Database Connection Issues
- Verify PostgreSQL is running
- Check connection string in `appsettings.json`
- Ensure database exists or let migrations create it

### Migration Errors
- Ensure all model changes are saved
- Try removing the last migration: `dotnet ef migrations remove`
- Recreate migration if needed

### Build Errors
- Run `dotnet restore` to restore packages
- Check for missing using statements
- Verify .NET 8.0 SDK is installed

---

## Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core Documentation](https://docs.microsoft.com/ef/core)
- [PostgreSQL Documentation](https://www.postgresql.org/docs/)

---

## License
This project is for educational purposes.
