using Educore_LMS.Data;
using Educore_LMS.DTOs;
using Educore_LMS.Models.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Educore_LMS.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            // Check if user already exists
            var userExists = await _userManager.FindByEmailAsync(registerDto.Email);
            if (userExists != null)
                return BadRequest("User already exists!");

            // Create new user
            ApplicationUser user = new()
            {
                Email = registerDto.Email,
                UserName = registerDto.Email,
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                SecurityStamp = Guid.NewGuid().ToString()
            };
            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Create role if it doesn't exist
            if (!await _roleManager.RoleExistsAsync(registerDto.Role))
                await _roleManager.CreateAsync(new IdentityRole(registerDto.Role));

            // Add user to role
            await _userManager.AddToRoleAsync(user, registerDto.Role);

            // Create role-specific profile
            if (registerDto.Role == "Student")
            {
                var student = new Student
                {
                    UserId = user.Id,
                    StudentNumber = registerDto.StudentNumber ?? GenerateStudentNumber(),
                    Name = registerDto.FirstName,
                    Surname = registerDto.LastName,
                    Gender = registerDto.Gender ?? "Unknown",
                    DateOfBirth = registerDto.DateOfBirth != default(DateTime) ? registerDto.DateOfBirth : DateTime.MinValue,
                    HomeAddress = registerDto.HomeAddress ?? "Not provided"
                };
                _context.Students.Add(student);
                await _context.SaveChangesAsync();
            }
            else if (registerDto.Role == "Lecturer")
            {
                var lecturer = new Lecturer
                {
                    LecturerId = 0,
                    UserId = user.Id,
                    StaffNumber = registerDto.StaffNumber ?? GenerateStaffNumber(),
                };
                _context.Lecturers.Add(lecturer);
            }

            await _context.SaveChangesAsync();

            return Ok("User created successfully!");
        }
            [HttpPost("login")]

            public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
            {
                var user = await _userManager.FindByEmailAsync(loginDto.Email);
                if (user == null || !await _userManager.CheckPasswordAsync(user, loginDto.Password))
                    return Unauthorized("Invalid credentials");

                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(3),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                return Ok(new UserResponseDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = userRoles.FirstOrDefault(),
                    Token = new JwtSecurityTokenHandler().WriteToken(token)
                });
            }
        
            private string GenerateStudentNumber()
        {
            return $"S{DateTime.Now:yyyyMMddHHmmss}";
        }

        private string GenerateStaffNumber()
        {
            return $"L{DateTime.Now:yyyyMMddHHmmss}";
        }
    
    }
    
}
