namespace HRsystem.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; } // Primary Key (PK)
        public required string UserName { get; set; }
        public required string PasswordHash { get; set; } // Hash qilingan parol
        public required string Role { get; set; } // "Admin" yoki "Employee"
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public string? ProfileImagePath { get; set; }
        public DateTime DateOfBirth { get; set; } = DateTime.MinValue;
        public DateTime HireDate { get; set; } = DateTime.MinValue;
        public required string Position { get; set; }
        public required string City { get; set; }
        public required string JobTitle { get; set; }
        public int DepartmentId { get; set; }
        public decimal Wallet { get; set; }
        public Department Department { get; set; }
        public WorkSchedule? WorkSchedule { get; set; } // Bog‘lanish
    }
}
