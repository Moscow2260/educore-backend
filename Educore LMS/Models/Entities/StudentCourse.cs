using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.Models.Entities
{
    public class StudentCourse
    {
        [Required]
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        [Required]
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
    }
}
