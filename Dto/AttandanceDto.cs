public class AttendanceDto
{
    public int AttendanceId { get; set; }
    public int EmployeeId { get; set; }
    public string? EmployeeName { get; set; } // Admin uchun
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public DateTime AttendanceDate { get; set; }
    public double HoursWorked { get; set; }
}

public class AttendanceEntryDto
{
    public int EmployeeId { get; set; }
}
