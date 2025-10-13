using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.Models.Entities
{
    public class Course
    {
        [Key]
        public required int CourseId { get; set; }
        public required string Name { get; set; } = string.Empty;
        public  required string Description { get; set; } = string.Empty; 

        //Foreignkey to lecturer
        public int? LecturerId { get; set; }
        public Lecturer? Lecturer { get; set; }

        //Relationships
        public ICollection<Module> Modules { get; set; } = new List<Module>();
        public ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
    }
}
