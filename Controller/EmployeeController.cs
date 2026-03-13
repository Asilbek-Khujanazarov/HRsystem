using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using HRsystem.Data;
using HRsystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace HRsystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        private readonly IMapper _mapper;

        public EmployeeController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IWebHostEnvironment environment,
            IMapper mapper
        )
        {
            _context = context;
            _configuration = configuration;
            _environment = environment;
            _mapper = mapper;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] EmployeeRegisterDto dto)
        {
            if (_context.Employees.Any(e => e.UserName == dto.UserName))
                return BadRequest(new { Message = "Bu username allaqachon mavjud." });

            if (!_context.Departments.Any(d => d.DepartmentId == dto.DepartmentId))
                return BadRequest(new { Message = "Bunday bo‘lim topilmadi." });

            var employee = _mapper.Map<Employee>(dto);
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    Message = "Xodim muvaffaqiyatli ro‘yxatdan o‘tdi.",
                    EmployeeId = employee.EmployeeId,
                }
            );
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] EmployeeLoginDto dto)
        {
            var employee = await _context.Employees.FirstOrDefaultAsync(e =>
                e.UserName == dto.UserName
            );

            if (employee == null || !BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash))
                return Unauthorized(new { Message = "Noto‘g‘ri username yoki parol." });

            var token = GenerateJwtToken(employee);
            return Ok(
                new
                {
                    EmployeeId = employee.EmployeeId,
                    Username = employee.UserName,
                    Role = employee.Role,
                    Token = token,
                }
            );
        }

        [HttpGet("employee-info/{id?}")]
        [Authorize]
        public async Task<IActionResult> GetEmployeeInfo(int? id)
        {
            int employeeId;

            if (id.HasValue)
            {
                // Token foydalanuvchisi admin emasligini tekshiramiz
                var isAdmin = User.IsInRole("Admin");

                if (!isAdmin)
                {
                    return Forbid(
                        "Faqat adminlar boshqa xodimlarning ma'lumotlarini ko'rishlari mumkin."
                    );
                }

                employeeId = id.Value;
            }
            else
            {
                // ID yo‘q bo‘lsa, token ichidagi foydalanuvchi IDni olamiz
                employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            }

            var employee = await _context
                .Employees.Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return NotFound(new { Message = "Xodim topilmadi." });

            var employeeDto = _mapper.Map<EmployeeDto>(employee);
            return Ok(employeeDto);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllEmployees([FromQuery] string? role = null)
        {
            var query = _context.Employees.Include(e => e.Department).AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(e => e.Role == role);

            var employees = await query.ToListAsync();
            var employeeDtos = _mapper.Map<List<EmployeeDto>>(employees);
            return Ok(employeeDtos);
        }

        [HttpGet("[Action]")]
        public async Task<IActionResult> GetEmployeeNameId(int id)
        {
            var employee = await _context
                .Employees.Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            var employeeNameDto = _mapper.Map<EmployeeNameDto>(employee);
            return Ok(employeeNameDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEmployee(int id, [FromBody] Employee updatedEmployee)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            _mapper.Map(updatedEmployee, employee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee deleted successfully" });
        }

        [HttpPut("upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = "Foydalanuvchi ID si topilmadi." });

            int employeeId = int.Parse(userIdClaim);
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest(
                    new { message = "Invalid file type. Allowed: .jpg, .jpeg, .png" }
                );

            if (!string.IsNullOrEmpty(employee.ProfileImagePath))
            {
                string oldFilePath = Path.Combine(
                    _environment.WebRootPath,
                    employee.ProfileImagePath.TrimStart('/')
                );
                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
            }

            string fileName = $"{Guid.NewGuid()}{fileExtension}";
            string filePath = Path.Combine(_environment.WebRootPath, "images", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            employee.ProfileImagePath = $"/images/{fileName}";
            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    message = "Profile image updated successfully",
                    imagePath = employee.ProfileImagePath,
                }
            );
        }

        [HttpGet("get-profile-image")]
        public async Task<IActionResult> GetProfileImage()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = "Foydalanuvchi ID si topilmadi." });

            var employeeId = int.Parse(userIdClaim);
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            string imagePath = employee.ProfileImagePath ?? "/images/default.png";
            return Ok(new { imagePath });
        }

        [HttpDelete("delete-profile-image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { Message = "Foydalanuvchi ID si topilmadi." });

            int employeeId = int.Parse(userIdClaim);
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            if (string.IsNullOrEmpty(employee.ProfileImagePath))
                return BadRequest(new { message = "Profile image not found" });

            string filePath = Path.Combine(
                _environment.WebRootPath,
                employee.ProfileImagePath.TrimStart('/')
            );
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            employee.ProfileImagePath = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile image deleted successfully" });
        }

        private string GenerateJwtToken(Employee employee)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, employee.EmployeeId.ToString()),
                new Claim(ClaimTypes.Name, employee.UserName),
                new Claim(ClaimTypes.Role, employee.Role),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
