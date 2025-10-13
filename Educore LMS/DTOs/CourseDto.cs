using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.DTOs
{
    public class CourseDto
    {
        public required int CourseId { get; set; }
        public required string Name { get; set; } = string.Empty;
        public required string Description { get; set; } = string.Empty;
        public int? LecturerId { get; set; }
        public string? LecturerName { get; set; }
        public List<ModuleDto> Modules { get; set; } = new();
    }

    public class CreateCourseDto
    {
        public required string Name { get; set; } = string.Empty;
        public required string Description { get; set; } = string.Empty;

        public int? LecturerId { get; set; }
    }
    public class UpdateCourseDto
    {
        public required string Name { get; set; } = string.Empty;

        public required string Description { get; set; } = string.Empty;

        public int? LecturerId { get; set; }
    }
    
}
