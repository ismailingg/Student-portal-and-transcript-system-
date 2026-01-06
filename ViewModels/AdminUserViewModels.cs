using System.ComponentModel.DataAnnotations;

namespace WP_project.ViewModels
{
    public class CreateStudentViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string FullName { get; set; } = null!;

        [Required]
        public string RollNumber { get; set; } = null!;

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }

    public class CreateFacultyViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string FullName { get; set; } = null!;

        [Required]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = null!;
    }

    public class ChangePasswordViewModel
    {
        public int UserId { get; set; }
        
        public string UserName { get; set; } = null!;
        
        public string UserEmail { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        public string NewPassword { get; set; } = null!;

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }
}

