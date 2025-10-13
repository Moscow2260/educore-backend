using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.DTOs
{
    public class ModuleTaskDto
    {
        public int TaskId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int ModuleId { get; set; }
        public string ModuleName { get; set; } = string.Empty;
        public string Status { get; set; } = "Not Started"; // For student view
    }
    public class UpdateTaskDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }
    }
    public class CreateTaskDto
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }
    }

    public class CourseWithModulesDto
    {
        public int CourseId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<ModuleDto> Modules { get; set; } = new List<ModuleDto>();
    }
    public class UpdateTaskStatusRequest
    {
        [Required(ErrorMessage = "Status is required")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; }
    }
}
