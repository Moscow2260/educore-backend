using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.Models.Entities
{
    public class Student 
    {
        [Key]
        public int StudentId { get; set; }
        [Required]
        public string StudentNumber { get; set; } = string.Empty;
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string Surname { get; set; } = string.Empty;
        [Required]
        public string Gender { get; set; } = string.Empty;
        [Required]
        public DateTime DateOfBirth { get; set; }
        [Required]
        public string HomeAddress { get; set; } = string.Empty;
        [Required]
        public string EmailAddress { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }

        //Foreign key to ApplicationUser
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        //Relationships
        public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
        public ICollection<StudentTask> StudentTasks { get; set; } = new List<StudentTask>();
    }
}
