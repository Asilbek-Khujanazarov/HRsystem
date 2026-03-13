namespace HRsystem.Models
{
    public class Attendance
    {
        public int AttendanceId { get; set; } // Primary Key (PK)
        public int EmployeeId { get; set; } // Foreign Key (FK) - Employee jadvaliga bog‘lanadi
        public DateTime EntryTime { get; set; } // Turnikadan kirish vaqti
        public DateTime? ExitTime { get; set; } // Turnikadan chiqish vaqti (nullable, agar hali chiqmagan bo‘lsa)
        public DateTime AttendanceDate { get; set; } // Yo‘qlama sanasi
        public Employee Employee { get; set; }
        public decimal HoursWorked { get; internal set; }
    }
}
