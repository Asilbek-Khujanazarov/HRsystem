using System.Threading.Tasks;
using AutoMapper;
using HRsystem.Data;
using HRsystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRsystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobApplicationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IMapper _mapper;

        public JobApplicationController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IMapper mapper
        )
        {
            _context = context;
            _environment = environment;
            _mapper = mapper;
        }

        // POST: api/jobapplication (Yangi ariza qo‘shish)
        [HttpPost]
        public async Task<IActionResult> AddJobApplication(
            [FromForm] string firstName,
            [FromForm] string lastName,
            [FromForm] string email,
            [FromForm] string route,
            [FromForm] List<IFormFile> jobApplications
        )
        {
            if (jobApplications.Count > 5)
            {
                return BadRequest("Eng ko‘pi bilan 5 ta fayl yuklash mumkin!");
            }
            string uploadPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // yangi rezume  yaratamiz\\
            var jobApplication = new JobApplication
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Route = route,
            };

            foreach (var file in jobApplications)
            {
                string fileName = $"{Guid.NewGuid()}_{file.FileName}";
                string filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var jobApplicationFile = new JobApplicationFile
                {
                    FileName = fileName,
                    FilePath = $"/uploads/{fileName}",
                };

                jobApplication.Files.Add(jobApplicationFile);
            }
            _context.JobApplications.Add(jobApplication);
            await _context.SaveChangesAsync();

            var jobApplicationDto = _mapper.Map<JobApplicationDto>(jobApplication);
            return Ok(new { Massage = "Ariza saqlandi", JobApplication = jobApplicationDto });
        }

        [HttpGet("[Action]")]
        // [Authorize(Roles = "Admin")]
        public IActionResult GetApplications()
        {
            var jobApplications = _context.JobApplications.Include(r => r.Files).ToList();
            var jobApplicationDto = _mapper.Map<List<JobApplicationDto>>(jobApplications);
            return Ok(jobApplicationDto);
        }

        [HttpPut("[Action]")]
        // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ChangeStatus(int id, string status)
        {
            var jobApplication = await _context.JobApplications.FindAsync(id);
            jobApplication.Status = status;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJobApplication(int id)
        {
            // Job application ni topamiz
            var jobApplication = await _context
                .JobApplications.Include(ja => ja.Files) // Fayllarni ham olib kelish
                .FirstOrDefaultAsync(ja => ja.Id == id);

            if (jobApplication == null)
            {
                return NotFound("Ariza topilmadi");
            }

            // Fayllarni o'chirish
            foreach (var file in jobApplication.Files)
            {
                string filePath = Path.Combine(
                    _environment.WebRootPath,
                    file.FilePath.TrimStart('/')
                );

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath); // Faylni o'chirish
                }
            }

            // Job application ni va unga bog'langan fayllarni o'chirish
            _context.JobApplications.Remove(jobApplication);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Ariza va fayllari o'chirildi" });
        }
    }
}
