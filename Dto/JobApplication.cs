using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRsystem.Models
{
    public class JobApplicationDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Route { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public List<JobApplicationFileDto> Files { get; set; } = new();
    }

    public class JobApplicationFileDto
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}
