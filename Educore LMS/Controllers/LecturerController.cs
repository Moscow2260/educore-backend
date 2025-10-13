using Educore_LMS.Data;
using Educore_LMS.DTOs;
using Educore_LMS.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Educore_LMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Lecturer")]
    public class LecturerController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LecturerController(ApplicationDbContext context)
        {
            _context = context;
           
        }

        //Get: api/Lecturer/my-course
        [HttpGet("my-course")]
        public async Task<ActionResult<IEnumerable<CourseWithModulesDto>>> GetMyCourses()
        {
            try
            {
                // Get the logged-in lecturer's ID from the user claims
                var lecturerId = GetCurrentLecturerId();
                if (lecturerId == null)
                    return Unauthorized("Lecturer not found.");

                var courses = await _context.Courses
                    .Where(c => c.LecturerId == lecturerId)
                    .Include(c => c.Modules)
                    .Select(c => new CourseWithModulesDto
                    {
                        CourseId = c.CourseId,
                        Name = c.Name,
                        Description = c.Description,
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
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while retrieving your courses.");
            }
        }
        // GET: api/Lecturer/my-courses/{courseId}/modules
        [HttpGet("my-courses/{courseId}/modules")]
        public async Task<ActionResult<IEnumerable<ModuleDto>>> GetMyCourseModules(int courseId)
        {
            try
            {
                //Get logged-in lecturer's Id from the user claims
                var lecturerId = GetCurrentLecturerId();
                if (lecturerId == null)
                    return Unauthorized("Lecturer not found.");

                //Verify that the course belongs to the lecturer
                var course = await _context.Courses
                    .FirstOrDefaultAsync(c => c.CourseId == courseId && c.LecturerId == lecturerId);

                if (course == null)
                    return NotFound("Course not found or you don't have access to it.");

                var modules = await _context.Modules
                    .Where(m => m.CourseId == courseId)
                    .Select(m => new ModuleDto
                    {
                        ModuleId = m.ModuleId,
                        Name = m.Name,
                        Description = m.Description,
                        CourseId = m.CourseId

                    })
                    .ToListAsync();

                return Ok(modules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occured while retrieving course module.");
            }
        }
        // GET: api/Lecturer/modules/{moduleId}/tasks
        [HttpGet("modules/{moduleId}/tasks")]
        public async Task<ActionResult<IEnumerable<ModuleTaskDto>>> GetModuleTasks(int moduleId)
        {
            try
            {
                // Get the logged-in lecturer's ID from the user claims
                var lecturerId = GetCurrentLecturerId();
                if (lecturerId == null)
                    return Unauthorized("Lecturer not found.");

                // Verify that the module belongs to a course assigned to the lecturer
                var module = await _context.Modules
                    .Include(m => m.Course)
                    .FirstOrDefaultAsync(m => m.ModuleId == moduleId && m.Course.LecturerId == lecturerId);

                if (module == null)
                    return NotFound("Module not found or you don't have access to it.");

                var tasks = await _context.ModuleTasks
                    .Where(t => t.ModuleId == moduleId)
                    .Select(t => new ModuleTaskDto
                    {
                        TaskId = t.ModuleTaskId,
                        Name = t.Name,
                        Description = t.Description, 
                        DueDate = t.DueDate,
                        ModuleId = t.ModuleId,
                        ModuleName = t.Module.Name
                    })
                    .ToListAsync();

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occured while retrieving module tasks");
            }
        }
        // POST: api/Lecturer/modules/{moduleId}/tasks
        [HttpPost("modules/{moduleId}/tasks")]
        public async Task<ActionResult<ModuleTaskDto>> CreateTask(int moduleId, [FromBody] CreateTaskDto taskDto)
        {
            try
            {
                // Get the logged-in lecturer's ID from the user claims
                var lecturerId = GetCurrentLecturerId();
                if (lecturerId == null)
                    return Unauthorized("Lecturer not found.");

                // Verify that the module belongs to a course assigned to the lecturer
                var module = await _context.Modules
                    .Include(m => m.Course)
                    .FirstOrDefaultAsync(m => m.ModuleId == moduleId && m.Course.LecturerId == lecturerId);

                if (module == null)
                    return NotFound("Module not found or you don't have access to it.");

                // Validate due date is in the future
                if (taskDto.DueDate <= DateTime.UtcNow)
                    return BadRequest("Due date must be in the future.");

                var task = new ModuleTask
                {
                    Name = taskDto.Name,
                    Description = taskDto.Description,
                    DueDate = taskDto.DueDate,
                    ModuleId = moduleId
                };

                _context.ModuleTasks.Add(task);
                await _context.SaveChangesAsync();

                // Return the created task
                return CreatedAtAction(nameof(GetModuleTasks), new { moduleId = moduleId }, new ModuleTaskDto
                {
                    TaskId = task.ModuleTaskId,
                    Name = task.Name,
                    Description = task.Description,
                    DueDate = task.DueDate,
                    ModuleId = task.ModuleId,
                    ModuleName = module.Name
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the task.");
            }
        }
        // PUT: api/Lecturer/tasks/{taskId}
        [HttpPut("tasks/{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] UpdateTaskDto taskDto)
        {
            try
            {
                // Get the logged-in lecturer's ID from the user claims
                var lecturerId = GetCurrentLecturerId();
                if (lecturerId == null)
                    return Unauthorized("Lecturer not found.");

                // Verify that the task belongs to a module that belongs to a course assigned to the lecturer
                var task = await _context.ModuleTasks
                    .Include(t => t.Module)
                    .ThenInclude(m => m.Course)
                    .FirstOrDefaultAsync(t => t.ModuleTaskId == taskId && t.Module.Course.LecturerId == lecturerId);

                if (task == null)
                    return NotFound("Task not found or you don't have access to it.");

                // Validate due date is in the future
                if (taskDto.DueDate <= DateTime.UtcNow)
                    return BadRequest("Due date must be in the future.");

                task.Name = taskDto.Name;
                task.Description = taskDto.Description;
                task.DueDate = taskDto.DueDate;

                _context.Entry(task).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the task.");
            }
        }
        // DELETE: api/Lecturer/tasks/{taskId}
        [HttpDelete("tasks/{taskId}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            try
            {
                // Get the logged-in lecturer's ID from the user claims
                var lecturerId = GetCurrentLecturerId();
                if (lecturerId == null)
                    return Unauthorized("Lecturer not found.");

                // Verify that the task belongs to a module that belongs to a course assigned to the lecturer
                var task = await _context.ModuleTasks
                    .Include(t => t.Module)
                    .ThenInclude(m => m.Course)
                    .FirstOrDefaultAsync(t => t.ModuleTaskId == taskId && t.Module.Course.LecturerId == lecturerId);

                if (task == null)
                    return NotFound("Task not found or you don't have access to it.");

                _context.ModuleTasks.Remove(task);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while deleting the task.");
            }
        }












        //Helper method to get current lecturer ID from claim
        private int? GetCurrentLecturerId()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return null;

            var lecturer = _context.Lecturers.FirstOrDefault(l => l.UserId == userId);
            return lecturer?.LecturerId;
        }

    }
}
