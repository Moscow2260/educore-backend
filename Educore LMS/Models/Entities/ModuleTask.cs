using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.Models.Entities
{
    public class ModuleTask
    {
        [Key]
        public int ModuleTaskId { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Status { get; set; }
        [Required]
        public DateTime DueDate { get; set; }

        //Foreign key to Module
        [Required]
        public int ModuleId { get; set; }
        public Module Module { get; set; } = null!;

        //Relationships
        public ICollection<StudentTask> StudentTasks = new List<StudentTask>();

    }
}
