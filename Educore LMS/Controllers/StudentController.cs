using Educore_LMS.Data;
using Educore_LMS.DTOs;
using Educore_LMS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Claims;

namespace Educore_LMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<StudentController> _logger;

        public StudentController(ApplicationDbContext context,
                               UserManager<ApplicationUser> userManager,
                               ILogger<StudentController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }
            // GET: api/student/tasks
            [HttpGet("tasks")]
            public async Task<IActionResult> GetStudentTasks([FromQuery] string? status = null)
            {
                try
                {
                    _logger.LogInformation("Attempting to get tasks for student");

                    var student = await GetCurrentStudentAsync();
                    if (student == null)
                    {
                        _logger.LogWarning("Student not found for user: {UserId}", _userManager.GetUserId(User));
                        return Unauthorized(new { message = "Student profile not found" });
                    }

                    _logger.LogInformation("Found student: {StudentId}", student.StudentId);

                    // First, get the student's course IDs
                    var studentCourseIds = await _context.StudentCourses
                        .Where(sc => sc.StudentId == student.StudentId)
                        .Select(sc => sc.CourseId)
                        .ToListAsync();

                    _logger.LogInformation("Student is enrolled in {Count} courses", studentCourseIds.Count);

                    if (!studentCourseIds.Any())
                    {
                        return Ok(new List<ModuleTaskDto>()); // Return empty list if no courses
                    }

                    // Get module IDs for the student's courses
                    var moduleIds = await _context.Modules
                        .Where(m => studentCourseIds.Contains(m.CourseId))
                        .Select(m => m.ModuleId)
                        .ToListAsync();

                    _logger.LogInformation("Found {Count} modules for student", moduleIds.Count);

                    var query = _context.ModuleTasks
                        .Include(t => t.Module)
                            .ThenInclude(m => m.Course)
                        .Where(t => moduleIds.Contains(t.ModuleId));

                    // Filter by status if provided
                    if (!string.IsNullOrEmpty(status) && status != "All")
                    {
                        query = query.Where(t => t.Status == status);
                    }

                    var tasks = await query
                        .OrderBy(t => t.DueDate)
                        .Select(t => new ModuleTaskDto
                        {
                            TaskId = t.ModuleTaskId,
                            Name = t.Name,
                            Description = t.Description ?? string.Empty,
                            DueDate = t.DueDate,
                            ModuleId = t.ModuleId,
                            ModuleName = t.Module.Name,
                            Status = t.Status?? "Not Started"
                        })
                        .ToListAsync();

                    _logger.LogInformation("Returning {Count} tasks for student", tasks.Count);
                    return Ok(tasks);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error retrieving tasks for student: {Message}", ex.Message);
                    return StatusCode(500, new { message = "An internal error occurred while retrieving tasks" });
                }
            }

        // PUT: api/student/tasks/{id}/status
        [HttpPut("tasks/{id}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting to update task {TaskId} status to {Status}", id, request.Status);

                if (request == null || string.IsNullOrEmpty(request.Status))
                {
                    return BadRequest(new { message = "Status is required" });
                }

                var student = await GetCurrentStudentAsync();
                if (student == null)
                {
                    return Unauthorized(new { message = "Student profile not found" });
                }

                // Get student's course IDs
                var studentCourseIds = await _context.StudentCourses
                    .Where(sc => sc.StudentId == student.StudentId)
                    .Select(sc => sc.CourseId)
                    .ToListAsync();

                // Find the task and verify it belongs to student's modules
                var task = await _context.ModuleTasks
                    .Include(t => t.Module)
                    .FirstOrDefaultAsync(t => t.ModuleTaskId == id &&  // search by task ID
                            studentCourseIds.Contains(t.Module.CourseId));


                if (task == null)
                {
                    _logger.LogWarning("Task {TaskId} not found or access denied for student {StudentId}", id, student.StudentId);
                    return NotFound(new { message = "Task not found or you don't have access to it" });
                }

                // Validate status
                var validStatuses = new[] { "Not Started", "In Progress", "Complete" };
                if (!validStatuses.Contains(request.Status))
                {
                    return BadRequest(new
                    {
                        message = "Invalid status. Must be: Not Started, In Progress, or Complete",
                        validStatuses
                    });
                }

                task.Status = request.Status;
                //task.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Task {TaskId} status updated successfully to {Status}", id, request.Status);
                return Ok(new { message = "Task status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task status: {Message}", ex.Message);
                return StatusCode(500, new { message = "An internal error occurred while updating task status" });
            }
        }

        private async Task<Student?> GetCurrentStudentAsync()
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var allStudents = await _context.Students.ToListAsync();
                foreach (var s in allStudents)
                {
                    if (s.UserId.Trim() == userId.Trim())
                    {
                        _logger.LogInformation($"MATCH FOUND: StudentId: {s.StudentId}, UserId: '{s.UserId}'");
                    }
                }
   
                _logger.LogInformation($"Searching for student profile with UserId: {userId}");

                // Log all students for debugging
             
                foreach (var s in allStudents)
                {
                    _logger.LogInformation($"StudentId: {s.StudentId}, UserId: '{s.UserId}'");
                }

                // Trim UserId to avoid hidden spaces
                if (userId == null)
                {
                    _logger.LogWarning("UserId is null");
                    return null;
                }

                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.UserId != null && s.UserId.Trim() == userId.Trim());

                if (student == null)
                {
                    _logger.LogWarning("No student record found for user ID: {UserId}", userId);
                }

                return student;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current student");
                return null;
            }
        }





        // GET: api/Student/tasks/status-options
        [HttpGet("tasks/status-options")]
        public ActionResult<IEnumerable<string>> GetTaskStatusOptions()
        {
            return Ok(new[] { "Not Started", "In Progress", "Complete" });
        }




        // Helper method to validate task status
        private bool IsValidStatus(string status)
        {
            var validStatuses = new[] { "Not Started", "In Progress", "Complete" };
            return validStatuses.Contains(status);
        }

        // Helper method to get current student ID from claims
        private int? GetCurrentStudentId()
        {

            try
            {
                // Get the current user's ID from claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine("User ID not found in claims");
                    return null;
                }

                Console.WriteLine($"Current User ID: {userId}");

                // Find the student record linked to this user
                var student = _context.Students
                    .FirstOrDefault(s => s.UserId == userId || s.UserId == userId); // Try different property names

                if (student == null)
                {
                    Console.WriteLine($"No student record found for User ID: {userId}");
                    Console.WriteLine($"Total students in DB: {_context.Students.Count()}");

                    // Log all students for debugging
                    var allStudents = _context.Students.ToList();
                    foreach (var s in allStudents)
                    {
                        Console.WriteLine($"Student: {s.StudentId}, UserId: {s.UserId}");
                    }
                }
                else
                {
                    Console.WriteLine($"Found student: {student.StudentId} for user: {userId}");
                }

                return student?.StudentId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetCurrentStudentId: {ex.Message}");
                return null;
            }
        }
    }
}
