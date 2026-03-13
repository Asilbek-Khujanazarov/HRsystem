// Ish jadvalini yaratish/yangilash uchun
public class WorkScheduleDto
{
    public int EmployeeId { get; set; }
    public string WorkStartTime { get; set; } // "HH:mm"
    public string WorkEndTime { get; set; }   // "HH:mm"
    public string WorkDays { get; set; }      // "Mon,Tue,Wed"
    public decimal HourlyRate { get; set; }
}

// Maosh qo‘shish uchun
public class PayrollCreateDto
{
    public int EmployeeId { get; set; }
    public DateTime PayDate { get; set; }
}

// Maosh tasdiqlash uchun
public class PayrollConfirmDto
{
    public int PayrollId { get; set; }
    public bool IsPaid { get; set; }
}

// Xodimning o‘z hisoboti uchun
public class MyPayrollDto
{
    public int PayrollId { get; set; }
    public DateTime PayDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public double TotalHoursWorked { get; set; }
}

// Admin uchun har bir xodimning hisoboti
public class EmployeePayrollDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public int PayrollId { get; set; }
    public DateTime PayDate { get; set; }
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidDate { get; set; }
    public double TotalHoursWorked { get; set; }
}

// Umumiy hisobot uchun
public class GeneralReportDto
{
    public int TotalEmployees { get; set; }
    public decimal TotalEarned { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalUnpaid { get; set; }
    public List<EmployeePayrollDto> Payrolls { get; set; }
}