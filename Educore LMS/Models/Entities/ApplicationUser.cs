using Microsoft.AspNetCore.Identity;

namespace Educore_LMS.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        //Navigation properties
        public Student? Student { get; set; }
        public Lecturer? Lecturer { get; set; }
    }
}
