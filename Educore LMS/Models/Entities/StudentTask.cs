using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.Models.Entities
{
    public class StudentTask
    {
        [Required]
        public int StudentId { get; set; }
        public Student Student { get; set; } = null!;

        required
        public int ModuleTaskId { get; set; }
        public ModuleTask ModuleTask { get; set; } = null!;

        [Required]
        public string Status { get; set; } = "Not Started";
       
    }
}
