using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.DTOs
{
    public class StudentDto
    {
        public required int StudentId { get; set; }
        public required string StudentNumber { get; set; } = string.Empty;
        public required string Name { get; set; } = string.Empty;
        public  required string Surname { get; set; } = string.Empty;
        public required string Gender { get; set; } = string.Empty;
        public  required DateTime DateOfBirth { get; set; }
        public required string HomeAddress { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
    }

    public class CreateStudentDto
    {
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string HomeAddress { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    public class UpdateStudentDto
    {
        public string StudentNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string HomeAddress { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
}
public class StudentTaskDto
{
    public int TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Status { get; set; } = "Not Started";
    public DateTime? LastUpdated { get;  set; }
}
public class UpdateTaskStatusDto
{
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Not Started";
}
