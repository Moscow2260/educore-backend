using System.ComponentModel.DataAnnotations;

namespace Educore_LMS.Models.Entities
{
    public class Lecturer 
    {
        [Key]
        public required int LecturerId { get; set; }
        public required string StaffNumber { get; set; } = string.Empty;

        //Foreign key to ApplicationUser 
        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        //Relationships
        public ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
