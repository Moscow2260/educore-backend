using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.Models.Entities
{
    public class Module
    {
        [Key]
        public int ModuleId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        //Foreign key to Course
        [Required]
        public int CourseId { get; set; }
        public Course Course { get; set; } = null!;

        //Relationships
        public ICollection<ModuleTask> Tasks = new List<ModuleTask>();
    }
}
