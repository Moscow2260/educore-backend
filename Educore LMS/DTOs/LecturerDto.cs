namespace Educore_LMS.DTOs
{
    public class LecturerDto
    {
        public required int LecturerId { get; set; }
        public required string StaffNumber { get; set; } = string.Empty;
        public required string Name { get; set; } = string.Empty;
        public required string Surname { get; set; } = string.Empty;
        public required string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }
    public class CreateLecturerDto
    {
        public  required string StaffNumber { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class UpdateLecturerDto
    {
        public required string StaffNumber { get; set; }
        public required string Name { get; set; }
        public required string Surname { get; set; }
        public required string Email { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
