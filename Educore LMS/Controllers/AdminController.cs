using Educore_LMS.Data;
using Educore_LMS.DTOs;
using Educore_LMS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Educore_LMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize (Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Admin/lecturers
        [HttpGet("lecturers")]
        public async Task<ActionResult<IEnumerable<LecturerDto>>> GetLecturers()
        {
            var lecturers = await _context.Lecturers
                .Include(l => l.User)
                .OrderBy(l => l.User.LastName)
                .Select(l => new LecturerDto
                {
                    LecturerId = l.LecturerId,
                    StaffNumber = l.StaffNumber,
                    Name = l.User.FirstName,
                    Surname = l.User.LastName,
                    Email = l.User.Email,
                    PhoneNumber = l.User.PhoneNumber
                })
                .ToListAsync();

            return Ok(lecturers);
        }

        //Get lecturer by id
        [HttpGet("lecturers/{id}")]
        public async Task<ActionResult<LecturerDto>> GetLecturer(int id)
        {
            var lecturer = await _context.Lecturers
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.LecturerId == id);

            if (lecturer == null)
                return NotFound();

            return new LecturerDto
            {
                LecturerId = lecturer.LecturerId,
                StaffNumber = lecturer.StaffNumber,
                Name = lecturer.User.FirstName,
                Surname = lecturer.User.LastName,
                Email = lecturer.User.Email,
                PhoneNumber = lecturer.User.PhoneNumber
            };
        }

        // POST: api/Admin/lecturers
        [HttpPost("lecturers")]
        public async Task<ActionResult<LecturerDto>> CreateLecturer([FromBody] CreateLecturerDto lecturerDto)
        {
            try
            {
                // Check if staff number already exists
                if (await _context.Lecturers.AnyAsync(l => l.StaffNumber == lecturerDto.StaffNumber))
                    return BadRequest("Staff number already exists.");

                // Check if email already exists in User table
                if (await _context.Users.AnyAsync(u => u.Email == lecturerDto.Email))
                    return BadRequest("Email address already exists.");

                // Create ApplicationUser first
                var user = new ApplicationUser
                {
                    UserName = lecturerDto.Email,
                    Email = lecturerDto.Email,
                    FirstName = lecturerDto.Name,
                    LastName = lecturerDto.Surname,
                    PhoneNumber = lecturerDto.PhoneNumber
                };

                // This would typically use UserManager, but for simplicity we're adding directly
                // In a real scenario, you would use UserManager.CreateAsync with password
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Lecturer profile
                var lecturer = new Lecturer
                {
                    LecturerId = 0, // or use a suitable value if you have a way to generate it, otherwise EF will set it on save
                    StaffNumber = lecturerDto.StaffNumber,
                    UserId = user.Id
                };

                _context.Lecturers.Add(lecturer);
                await _context.SaveChangesAsync();

                // Return the created lecturer
                return CreatedAtAction(nameof(GetLecturer), new { id = lecturer.LecturerId }, new LecturerDto
                {
                    LecturerId = lecturer.LecturerId,
                    StaffNumber = lecturer.StaffNumber,
                    Name = user.FirstName,
                    Surname = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the lecturer.");
            }
        }

        // PUT: api/Admin/lecturers/5
        [HttpPut("lecturers/{id}")]
        public async Task<IActionResult> UpdateLecturer(int id, [FromBody] UpdateLecturerDto lecturerDto)
        {
            try
            {
                var lecturer = await _context.Lecturers
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.LecturerId == id);

                if (lecturer == null)
                    return NotFound();

                // Check if staff number is being changed and if it already exists
                if (lecturer.StaffNumber != lecturerDto.StaffNumber &&
                    await _context.Lecturers.AnyAsync(l => l.StaffNumber == lecturerDto.StaffNumber && l.LecturerId != id))
                    return BadRequest("Staff number already exists.");

                // Check if email is being changed and if it already exists
                if (lecturer.User.Email != lecturerDto.Email &&
                    await _context.Users.AnyAsync(u => u.Email == lecturerDto.Email && u.Id != lecturer.UserId))
                    return BadRequest("Email address already exists.");

                // Update lecturer properties
                lecturer.StaffNumber = lecturerDto.StaffNumber;

                // Update user properties
                lecturer.User.FirstName = lecturerDto.Name;
                lecturer.User.LastName = lecturerDto.Surname;
                lecturer.User.Email = lecturerDto.Email;
                lecturer.User.UserName = lecturerDto.Email; // Keep username in sync with email
                lecturer.User.PhoneNumber = lecturerDto.PhoneNumber;

                _context.Entry(lecturer).State = EntityState.Modified;
                _context.Entry(lecturer.User).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LecturerExists(id))
                    return NotFound();
                else
                    throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the lecturer.");
            }
        }

        // DELETE: api/Admin/lecturers/5
        [HttpDelete("lecturers/{id}")]
        public async Task<IActionResult> DeleteLecturer(int id)
        {
            try
            {
                var lecturer = await _context.Lecturers
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.LecturerId == id);

                if (lecturer == null)
                    return NotFound();

                // Check if lecturer is assigned to any courses
                var hasCourses = await _context.Courses.AnyAsync(c => c.LecturerId == id);
                if (hasCourses)
                    return BadRequest("Cannot delete lecturer who is assigned to courses. Please reassign courses first.");

                // Remove lecturer and associated user
                _context.Lecturers.Remove(lecturer);
                _context.Users.Remove(lecturer.User);

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the lecturer.");
            }
        }

        

        private bool LecturerExists(int id)
        {
            return _context.Lecturers.Any(e => e.LecturerId == id);
        }

        // GET: api/Admin/students
        [HttpGet("students")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetStudents()
        {
            var students = await _context.Students
                .Include(s => s.User)
                .OrderBy(s => s.Surname)
                .Select(s => new StudentDto
                {
                    StudentId = s.StudentId,
                    StudentNumber = s.StudentNumber,
                    Name = s.Name,
                    Surname = s.Surname,
                    Gender = s.Gender,
                    DateOfBirth = s.DateOfBirth,
                    HomeAddress = s.HomeAddress,
                    Email = s.User.Email,
                    PhoneNumber = s.User.PhoneNumber
                })
                .ToListAsync();

            return Ok(students);
        }

        // GET: api/Admin/students/5
        [HttpGet("students/{id}")]
        public async Task<ActionResult<StudentDto>> GetStudent(int id)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
                return NotFound();

            return new StudentDto
            {
                StudentId = student.StudentId,
                StudentNumber = student.StudentNumber,
                Name = student.Name,
                Surname = student.Surname,
                Gender = student.Gender,
                DateOfBirth = student.DateOfBirth,
                HomeAddress = student.HomeAddress,
                Email = student.User.Email,
                PhoneNumber = student.User.PhoneNumber
            };
        }

        // POST: api/Admin/students
        [HttpPost("students")]
        public async Task<ActionResult<StudentDto>> CreateStudent([FromBody] CreateStudentDto studentDto)
        {
            try
            {
                // Check if student number already exists
                if (await _context.Students.AnyAsync(s => s.StudentNumber == studentDto.StudentNumber))
                    return BadRequest("Student number already exists.");

                // Check if email already exists in User table
                if (await _context.Users.AnyAsync(u => u.Email == studentDto.Email))
                    return BadRequest("Email address already exists.");

                // Create ApplicationUser first
                var user = new ApplicationUser
                {
                    UserName = studentDto.Email,
                    Email = studentDto.Email,
                    FirstName = studentDto.Name,
                    LastName = studentDto.Surname,
                    PhoneNumber = studentDto.PhoneNumber
                };

                // This would typically use UserManager, but for simplicity we're adding directly
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create Student profile
                var student = new Student
                {
                    StudentNumber = studentDto.StudentNumber,
                    Name = studentDto.Name,
                    Surname = studentDto.Surname,
                    Gender = studentDto.Gender,
                    DateOfBirth = studentDto.DateOfBirth,
                    HomeAddress = studentDto.HomeAddress,
                    UserId = user.Id
                };

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Return the created student
                return CreatedAtAction(nameof(GetStudent), new { id = student.StudentId }, new StudentDto
                {
                    StudentId = student.StudentId,
                    StudentNumber = student.StudentNumber,
                    Name = student.Name,
                    Surname = student.Surname,
                    Gender = student.Gender,
                    DateOfBirth = student.DateOfBirth,
                    HomeAddress = student.HomeAddress,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the student.");
            }
        }

        // PUT: api/Admin/students/5
        [HttpPut("students/{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto studentDto)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                    return NotFound();

                // Check if student number is being changed and if it already exists
                if (student.StudentNumber != studentDto.StudentNumber &&
                    await _context.Students.AnyAsync(s => s.StudentNumber == studentDto.StudentNumber && s.StudentId != id))
                    return BadRequest("Student number already exists.");

                // Check if email is being changed and if it already exists
                if (student.User.Email != studentDto.Email &&
                    await _context.Users.AnyAsync(u => u.Email == studentDto.Email && u.Id != student.UserId))
                    return BadRequest("Email address already exists.");

                // Update student properties
                student.StudentNumber = studentDto.StudentNumber;
                student.Name = studentDto.Name;
                student.Surname = studentDto.Surname;
                student.Gender = studentDto.Gender;
                student.DateOfBirth = studentDto.DateOfBirth;
                student.HomeAddress = studentDto.HomeAddress;

                // Update user properties
                student.User.FirstName = studentDto.Name;
                student.User.LastName = studentDto.Surname;
                student.User.Email = studentDto.Email;
                student.User.UserName = studentDto.Email; // Keep username in sync with email
                student.User.PhoneNumber = studentDto.PhoneNumber;

                _context.Entry(student).State = EntityState.Modified;
                _context.Entry(student.User).State = EntityState.Modified;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                    return NotFound();
                else
                    throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the student.");
            }
        }

        // DELETE: api/Admin/students/5
        [HttpDelete("students/{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            try
            {
                var student = await _context.Students
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.StudentId == id);

                if (student == null)
                    return NotFound();

                // Check if student is enrolled in any courses
                var hasEnrollments = await _context.StudentCourses.AnyAsync(sc => sc.StudentId == id);
                if (hasEnrollments)
                    return BadRequest("Cannot delete student who is enrolled in courses. Please remove enrollments first.");

                // Remove student and associated user
                _context.Students.Remove(student);
                _context.Users.Remove(student.User);

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the student.");
            }
        }
      
        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }

        // GET: api/Admin/courses
        [HttpGet("courses")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetCourses()
        {
            var courses = await _context.Courses
                .Include(c => c.Modules)
                .Select(c => new CourseDto
                {
                    CourseId = c.CourseId,
                    Name = c.Name,
                    Description = c.Description,
                    LecturerId = c.LecturerId,
                    Modules = c.Modules.Select(m => new ModuleDto
                    {
                        ModuleId = m.ModuleId,
                        Name = m.Name,
                        Description = m.Description,
                        CourseId = m.CourseId
                    }).ToList()
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET modules for a course
        [HttpGet("courses/{courseId}/modules")]
        public async Task<IActionResult> GetModulesByCourse(int courseId)
        {
            var modules = await _context.Modules
                .Where(m => m.CourseId == courseId)
                .Select(m => new {
                    m.ModuleId,
                    m.Name,
                    m.Description
                })
                .ToListAsync();

            return Ok(modules);
        }

        // POST: api/Admin/courses
        [HttpPost("courses")]
        public async Task<ActionResult<CourseDto>> CreateCourse([FromBody] CreateCourseDto courseDto)
        {
            try
            {
                // Check if course name already exists
                if (await _context.Courses.AnyAsync(c => c.Name.ToLower() == courseDto.Name.ToLower()))
                    return BadRequest("Course name already exists.");

                var course = new Course
                {
                    CourseId = 0,
                    Name = courseDto.Name,
                    Description = courseDto.Description,
                    LecturerId = courseDto.LecturerId
                };

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                return Ok(new CourseDto
                {
                    CourseId = course.CourseId,
                    Name = course.Name,
                    Description = course.Description,
                    LecturerId = course.LecturerId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the course.");
            }
        }

        // PUT: api/Admin/courses/5
        [HttpPut("courses/{id}")]
        public async Task<IActionResult> UpdateCourse(int id, [FromBody] UpdateCourseDto courseDto)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                    return NotFound();

                // Check if course name is being changed and if it already exists
                if (course.Name != courseDto.Name &&
                    await _context.Courses.AnyAsync(c => c.Name.ToLower() == courseDto.Name.ToLower() && c.CourseId != id))
                    return BadRequest("Course name already exists.");

                course.Name = courseDto.Name;
                course.Description = courseDto.Description;
                course.LecturerId = courseDto.LecturerId;

                _context.Entry(course).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the course.");
            }
        }

        // DELETE: api/Admin/courses/5
        [HttpDelete("courses/{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                    return NotFound();

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the course.");
            }
        }

        // POST: api/Admin/courses/{courseId}/modules
        [HttpPost("courses/{courseId}/modules")]
        public async Task<ActionResult<ModuleDto>> CreateModule(int courseId, [FromBody] CreateModuleDto moduleDto)
        {
            try
            {
                // Check if course exists
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                    return NotFound("Course not found.");

                // Check if module name already exists in this course
                if (await _context.Modules.AnyAsync(m => m.CourseId == courseId && m.Name.ToLower() == moduleDto.Name.ToLower()))
                    return BadRequest("Module name already exists in this course.");

                var module = new Module
                {
                    Name = moduleDto.Name,
                    Description = moduleDto.Description,
                    CourseId = courseId
                };

                _context.Modules.Add(module);
                await _context.SaveChangesAsync();

                return Ok(new ModuleDto
                {
                    ModuleId = module.ModuleId,
                    Name = module.Name,
                    Description = module.Description,
                    CourseId = module.CourseId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the module.");
            }
        }

        // PUT: api/Admin/modules/{id}
        [HttpPut("modules/{id}")]
        public async Task<IActionResult> UpdateModule(int id, [FromBody] UpdateModuleDto moduleDto)
        {
            try
            {
                var module = await _context.Modules.FindAsync(id);
                if (module == null)
                    return NotFound("Module not found.");

                // Check if module name is being changed and if it already exists in the same course
                if (module.Name != moduleDto.Name &&
                    await _context.Modules.AnyAsync(m => m.CourseId == module.CourseId && m.Name.ToLower() == moduleDto.Name.ToLower() && m.ModuleId != id))
                    return BadRequest("Module name already exists in this course.");

                module.Name = moduleDto.Name;
                module.Description = moduleDto.Description;

                _context.Entry(module).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the module.");
            }
        }

        // DELETE: api/Admin/modules/{id}
        [HttpDelete("modules/{id}")]
        public async Task<IActionResult> DeleteModule(int id)
        {
            try
            {
                var module = await _context.Modules
                    .Include(m => m.Tasks)
                    .FirstOrDefaultAsync(m => m.ModuleId == id);

                if (module == null)
                    return NotFound("Module not found.");

                // Check if module has tasks
                if (module.Tasks.Any())
                    return BadRequest("Cannot delete module that has tasks. Please delete the tasks first.");

                _context.Modules.Remove(module);
                await _context.SaveChangesAsync();
                
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the module.");
            }
        }

        // POST: api/Admin/courses/{courseId}/assign-lecturer/{lecturerId}
        [HttpPost("courses/{courseId}/assign-lecturer/{lecturerId}")]
        public async Task<IActionResult> AssignLecturerToCourse(int courseId, int lecturerId)
        {
            try
            {
                // Check if course exists
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                    return NotFound("Course not found.");

                // Check if lecturer exists
                var lecturer = await _context.Lecturers.FindAsync(lecturerId);
                if (lecturer == null)
                    return NotFound("Lecturer not found.");

                // Assign lecturer to course
                course.LecturerId = lecturerId;

                _context.Entry(course).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lecturer assigned to course successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while assigning lecturer to course.");
            }
        }

        // GET: api/Admin/courses/{courseId}/assigned-lecturer
        [HttpGet("courses/{courseId}/assigned-lecturer")]
        public async Task<ActionResult<LecturerDto>> GetAssignedLecturer(int courseId)
        {
            try
            {
                var course = await _context.Courses
                    .Include(c => c.Lecturer)
                    .ThenInclude(l => l.User)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                    return NotFound("Course not found.");

                if (course.Lecturer == null)
                    return NotFound("No lecturer assigned to this course.");

                return Ok(new LecturerDto
                {
                    LecturerId = course.Lecturer.LecturerId,
                    StaffNumber = course.Lecturer.StaffNumber,
                    Name = course.Lecturer.User.FirstName,
                    Surname = course.Lecturer.User.LastName,
                    Email = course.Lecturer.User.Email,
                    PhoneNumber = course.Lecturer.User.PhoneNumber
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving assigned lecturer.");
            }
        }

        // GET: api/Admin/lecturers/{lecturerId}/assigned-courses
        [HttpGet("lecturers/{lecturerId}/assigned-courses")]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetLecturerAssignedCourses(int lecturerId)
        {
            try
            {
                // Check if lecturer exists
                var lecturer = await _context.Lecturers.FindAsync(lecturerId);
                if (lecturer == null)
                    return NotFound("Lecturer not found.");

                var courses = await _context.Courses
                    .Where(c => c.LecturerId == lecturerId)
                    .Select(c => new CourseDto
                    {
                        CourseId = c.CourseId,
                        Name = c.Name,
                        Description = c.Description,
                        LecturerId = c.LecturerId
                    })
                    .ToListAsync();

                return Ok(courses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving lecturer's assigned courses.");
            }
        }

        // DELETE: api/Admin/courses/{courseId}/remove-lecturer
        [HttpDelete("courses/{courseId}/remove-lecturer")]
        public async Task<IActionResult> RemoveLecturerFromCourse(int courseId)
        {
            try
            {
                // Check if course exists
                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                    return NotFound("Course not found.");

                // Check if course has a lecturer assigned
                if (course.LecturerId == null)
                    return BadRequest("No lecturer is assigned to this course.");

                // Remove lecturer assignment
                course.LecturerId = null;

                _context.Entry(course).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lecturer removed from course successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while removing lecturer from course.");
            }
        }

        // ========== STUDENT ASSIGNMENT OPERATIONS ==========

        // POST: api/Admin/courses/{courseId}/assign-student/{studentId}
        [HttpPost("courses/{courseId}/assign-student/{studentId}")]
        public async Task<IActionResult> AssignStudentToCourse(int courseId, int studentId)
        {
            try
            {
                // Check if course exists
                var course = await _context.Courses
                    .Include(c => c.Modules)
                    .ThenInclude(m => m.Tasks)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                    return NotFound("Course not found.");

                // Check if student exists
                var student = await _context.Students.FindAsync(studentId);
                if (student == null)
                    return NotFound("Student not found.");

                // Check if already enrolled
                var existingEnrollment = await _context.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

                if (existingEnrollment != null)
                    return BadRequest("Student is already enrolled in this course.");

                // Create new course enrollment
                var studentCourse = new StudentCourse
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    EnrollmentDate = DateTime.UtcNow
                };

                _context.StudentCourses.Add(studentCourse);

                var studentTasks = new List<StudentTask>();

                foreach (var module in course.Modules)
                {
                    foreach (var task in module.Tasks)
                    {
                        // Avoid duplicates
                        var existingStudentTask = await _context.StudentTasks
                            .FirstOrDefaultAsync(st => st.StudentId == studentId && st.ModuleTaskId == task.ModuleTaskId);

                        if (existingStudentTask == null)
                        {
                            studentTasks.Add(new StudentTask
                            {
                                StudentId = studentId,
                                ModuleTaskId = task.ModuleTaskId,
                                Status = "Not Started" // default status
                            });
                        }
                    }
                }

                if (studentTasks.Any())
                    _context.StudentTasks.AddRange(studentTasks);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Student assigned to course successfully and enrolled in all related modules/tasks.",
                    totalModules = course.Modules.Count,
                    totalTasksAssigned = studentTasks.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        // POST: api/Admin/courses/{courseId}/assign-students
        [HttpPost("courses/{courseId}/assign-students")]
        public async Task<IActionResult> AssignMultipleStudentsToCourse(int courseId, [FromBody] int[] studentIds)
        {
            try
            {
                // Check if course exists and load modules + tasks
                var course = await _context.Courses
                    .Include(c => c.Modules)
                    .ThenInclude(m => m.Tasks)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                    return NotFound("Course not found.");

                var results = new List<object>();
                var newStudentCourses = new List<StudentCourse>();
                var newStudentTasks = new List<StudentTask>();

                foreach (var studentId in studentIds)
                {
                    // Check if student exists
                    var student = await _context.Students.FindAsync(studentId);
                    if (student == null)
                    {
                        results.Add(new { studentId, success = false, message = "Student not found." });
                        continue;
                    }

                    // Check if student already enrolled
                    var existingEnrollment = await _context.StudentCourses
                        .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

                    if (existingEnrollment != null)
                    {
                        results.Add(new { studentId, success = false, message = "Student already enrolled in this course." });
                        continue;
                    }

                    // Add course enrollment
                    newStudentCourses.Add(new StudentCourse
                    {
                        StudentId = studentId,
                        CourseId = courseId,
                        EnrollmentDate = DateTime.UtcNow
                    });

                    foreach (var module in course.Modules)
                    {
                        foreach (var task in module.Tasks)
                        {
                            // Avoid duplicates
                            var exists = await _context.StudentTasks
                                .AnyAsync(st => st.StudentId == studentId && st.ModuleTaskId == task.ModuleTaskId);

                            if (!exists)
                            {
                                newStudentTasks.Add(new StudentTask
                                {
                                    StudentId = studentId,
                                    ModuleTaskId = task.ModuleTaskId,
                                    Status = "Not Started"
                                });
                            }
                        }
                    }

                    results.Add(new { studentId, success = true, message = "Student assigned and tasks created." });
                }

                if (newStudentCourses.Any())
                    _context.StudentCourses.AddRange(newStudentCourses);

                if (newStudentTasks.Any())
                    _context.StudentTasks.AddRange(newStudentTasks);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Students assigned successfully.",
                    totalStudentsAssigned = newStudentCourses.Count,
                    totalTasksCreated = newStudentTasks.Count,
                    results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        // DELETE: api/Admin/courses/{courseId}/remove-student/{studentId}
        [HttpDelete("courses/{courseId}/remove-student/{studentId}")]
        public async Task<IActionResult> RemoveStudentFromCourse(int courseId, int studentId)
        {
            try
            {
                // Check if enrollment exists
                var enrollment = await _context.StudentCourses
                    .FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.CourseId == courseId);

                if (enrollment == null)
                    return NotFound("Student is not enrolled in this course.");

                // Load the course with its modules and tasks
                var course = await _context.Courses
                    .Include(c => c.Modules)
                    .ThenInclude(m => m.Tasks)
                    .FirstOrDefaultAsync(c => c.CourseId == courseId);

                if (course == null)
                    return NotFound("Course not found.");

                // Collect all ModuleTaskIds for this course
                var moduleTaskIds = course.Modules
                    .SelectMany(m => m.Tasks)
                    .Select(t => t.ModuleTaskId)
                    .ToList();

                // Find student's tasks related to this course
                var studentTasksToRemove = await _context.StudentTasks
                    .Where(st => st.StudentId == studentId && moduleTaskIds.Contains(st.ModuleTaskId))
                    .ToListAsync();

                // Remove the student-course link
                _context.StudentCourses.Remove(enrollment);

                // Remove all related student tasks
                if (studentTasksToRemove.Any())
                    _context.StudentTasks.RemoveRange(studentTasksToRemove);

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Student removed from course and related module tasks successfully.",
                    totalTasksRemoved = studentTasksToRemove.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }


        // GET: api/Admin/courses/{courseId}/enrolled-students
        [HttpGet("courses/{courseId}/enrolled-students")]
            public async Task<ActionResult<IEnumerable<StudentDto>>> GetEnrolledStudents(int courseId)
            {
                try
                {
                    // Check if course exists
                    var course = await _context.Courses.FindAsync(courseId);
                    if (course == null)
                        return NotFound("Course not found.");

                    var students = await _context.StudentCourses
                        .Where(sc => sc.CourseId == courseId)
                        .Include(sc => sc.Student)
                        .ThenInclude(s => s.User)
                        .Select(sc => new StudentDto
                        {
                            StudentId = sc.Student.StudentId,
                            StudentNumber = sc.Student.StudentNumber,
                            Name = sc.Student.Name,
                            Surname = sc.Student.Surname,
                            Gender = sc.Student.Gender,
                            DateOfBirth = sc.Student.DateOfBirth,
                            HomeAddress = sc.Student.HomeAddress,
                            Email = sc.Student.User.Email,
                            PhoneNumber = sc.Student.User.PhoneNumber,
                            EnrollmentDate = sc.EnrollmentDate
                        })
                        .ToListAsync();

                    return Ok(students);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "An error occurred while retrieving enrolled students.");
                }
            }

            // GET: api/Admin/students/{studentId}/enrolled-courses
            [HttpGet("students/{studentId}/enrolled-courses")]
            public async Task<ActionResult<IEnumerable<CourseDto>>> GetStudentEnrolledCourses(int studentId)
            {
                try
                {
                    // Check if student exists
                    var student = await _context.Students.FindAsync(studentId);
                    if (student == null)
                        return NotFound("Student not found.");

                    var courses = await _context.StudentCourses
                        .Where(sc => sc.StudentId == studentId)
                        .Include(sc => sc.Course)
                        .Select(sc => new CourseDto
                        {
                            CourseId = sc.Course.CourseId,
                            Name = sc.Course.Name,
                            Description = sc.Course.Description,
                            LecturerId = sc.Course.LecturerId
                        })
                        .ToListAsync();

                    return Ok(courses);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, "An error occurred while retrieving student's enrolled courses.");
                }
            }
     
    // GET: api/Admin/users/students
    [HttpGet("users/students")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> ListStudents()
        {
            return await GetStudents();
        }

        // GET: api/Admin/users/lecturers
        [HttpGet("users/lecturers")]
        public async Task<ActionResult<IEnumerable<LecturerDto>>> ListLecturers()
        {
            return await GetLecturers();
        }



        // GET: api/Admin/students/search?term=john
        [HttpGet("students/search")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> SearchStudents([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return await GetStudents();

                term = term.Trim().ToLower();

                var students = await _context.Students
                    .Include(s => s.User)
                    .Where(s =>
                        s.StudentNumber.ToLower().Contains(term) ||
                        s.Name.ToLower().Contains(term) ||
                        s.Surname.ToLower().Contains(term)
                    )
                    .OrderBy(s => s.Surname)
                    .Select(s => new StudentDto
                    {
                        StudentId = s.StudentId,
                        StudentNumber = s.StudentNumber,
                        Name = s.Name,
                        Surname = s.Surname,
                        Gender = s.Gender,
                        DateOfBirth = s.DateOfBirth,
                        HomeAddress = s.HomeAddress,
                        Email = s.User.Email,
                        PhoneNumber = s.User.PhoneNumber
                    })
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while searching for students.");
            }
        }

        // GET: api/Admin/lecturers/search?term=smith
        [HttpGet("lecturers/search")]
        public async Task<ActionResult<IEnumerable<LecturerDto>>> SearchLecturers([FromQuery] string term)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term))
                    return await GetLecturers();

                term = term.Trim().ToLower();

                var lecturers = await _context.Lecturers
                    .Include(l => l.User)
                    .Where(l =>
                        l.StaffNumber.ToLower().Contains(term) ||
                        l.User.FirstName.ToLower().Contains(term) ||
                        l.User.LastName.ToLower().Contains(term)
                    )
                    .OrderBy(l => l.User.LastName)
                     .Select(l => new LecturerDto
                     {
                         LecturerId = l.LecturerId,
                         StaffNumber = l.StaffNumber,
                         Name = l.User.FirstName,
                         Surname = l.User.LastName,
                         Email = l.User.Email,
                         PhoneNumber = l.User.PhoneNumber
                     })
                    .ToListAsync();

                return Ok(lecturers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while searching for lecturers.");
            }
        }
    }
}
