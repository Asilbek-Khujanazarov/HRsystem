using System.Security.Claims;
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
    public class PayrollController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public PayrollController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Ish jadvalini belgilash (Admin uchun)
        [HttpPost("schedule")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SetWorkSchedule([FromBody] WorkScheduleDto dto)
        {
            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
                return NotFound("Xodim topilmadi.");

            var schedule = await _context.WorkSchedules.FirstOrDefaultAsync(ws =>
                ws.EmployeeId == dto.EmployeeId
            );

            if (schedule == null)
            {
                schedule = new WorkSchedule { EmployeeId = dto.EmployeeId };
                _context.WorkSchedules.Add(schedule);
            }

            schedule.WorkStartTime = TimeSpan.Parse(dto.WorkStartTime);
            schedule.WorkEndTime = TimeSpan.Parse(dto.WorkEndTime);
            schedule.WorkDays = dto.WorkDays;
            schedule.HourlyRate = dto.HourlyRate;

            await _context.SaveChangesAsync();
            return Ok("Ish jadvali muvaffaqiyatli belgilandi.");
        }

        // Yangi: Maosh to‘lash (CRUD - Create)
        [HttpPost("pay")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PayPayroll([FromBody] PayPayrollDto dto)
        {
            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
                return NotFound("Xodim topilmadi.");

            if (dto.PaidAmount > employee.Wallet)
                return BadRequest("Xodimning balansida yetarli mablag‘ yo‘q.");

            var payroll = await _context.Payrolls.FirstOrDefaultAsync(p =>
                p.EmployeeId == dto.EmployeeId
                && p.PayDate.Month == dto.Month
                && p.PayDate.Year == dto.Year
            );
            // Payrollni qo‘shish yoki yangilash
            if (payroll == null)
            {
                payroll = new Payroll
                {
                    EmployeeId = dto.EmployeeId,
                    PayDate = new DateTime(dto.Year, dto.Month, 1),
                    Amount = dto.PaidAmount,
                    IsPaid = true,
                    PaidDate = DateTime.Now,
                };
                _context.Payrolls.Add(payroll);
            }
            else
            {
                payroll.IsPaid = true;
                payroll.PaidDate = DateTime.Now;
                payroll.Amount += dto.PaidAmount;
            }

            // Avval Payrollni saqlaymiz — ID generatsiya qilinadi
            await _context.SaveChangesAsync();

            // Endi Payment obyektini PayrollId bilan birga yaratsak bo‘ladi
            var payment = new Payment
            {
                EmployeeId = dto.EmployeeId,
                Amount = dto.PaidAmount,
                PaymentDate = DateTime.Now,
                PayrollId = payroll.PayrollId, // Endi bu yerda mavjud bo‘ladi
                Description = $"{dto.Month}-oy, {dto.Year}-yil uchun {dto.PaidAmount} $ to‘lov",
            };
            _context.Payments.Add(payment);

            // Xodimning balansidan yechib tashlaymiz
            employee.Wallet -= dto.PaidAmount;

            await _context.SaveChangesAsync();

            // Payment qaydiga PayrollId ni yangilash
            payment.PayrollId = payroll.PayrollId;
            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    EmployeeId = employee.EmployeeId,
                    PaidAmount = dto.PaidAmount,
                    NewWalletBalance = employee.Wallet,
                    PayrollStatus = new { payroll.IsPaid, payroll.PaidDate },
                    PaymentId = payment.PaymentId,
                    PaymentDate = payment.PaymentDate,
                }
            );
        }

        // Maosh to‘lovlarini ko‘rish (CRUD - Read)
        [HttpGet("payroll-history")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPayrollHistory(
            [FromQuery] int employeeId,
            [FromQuery] int month, // Oy majburiy
            [FromQuery] int year // Yil majburiy
        )
        {
            if (employeeId <= 0)
                return BadRequest(new { Message = "EmployeeId noto‘g‘ri kiritildi." });

            if (month < 1 || month > 12 || year < 1)
                return BadRequest(new { Message = "Oy yoki yil noto‘g‘ri kiritildi." });

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return NotFound(new { Message = "Xodim topilmadi." });

            var startOfMonth = new DateTime(year, month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            // Payroll qaydlari
            var payrolls = await _context
                .Payrolls.Where(p =>
                    p.EmployeeId == employeeId
                    && p.PayDate >= startOfMonth
                    && p.PayDate <= endOfMonth
                )
                .OrderBy(p => p.PaidDate)
                .Select(p => new
                {
                    PayrollId = p.PayrollId,
                    PayDate = p.PayDate,
                    Amount = p.Amount,
                    IsPaid = p.IsPaid,
                    PaidDate = p.PaidDate,
                })
                .ToListAsync();

            // Payment qaydlari
            var payments = await _context
                .Payments.Where(p =>
                    p.EmployeeId == employeeId
                    && p.PaymentDate >= startOfMonth
                    && p.PaymentDate <= endOfMonth
                )
                .OrderBy(p => p.PaymentDate)
                .Select(p => new
                {
                    PaymentId = p.PaymentId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    PayrollId = p.PayrollId,
                    Description = p.Description,
                })
                .ToListAsync();

            if (!payments.Any())
                return NotFound(
                    new
                    {
                        Message = $"Bu xodim uchun {month}-oy, {year}-yilda to‘lovlar topilmadi.",
                    }
                );

            // Statistika
            var totalPaidAmount = payments.Sum(p => p.Amount);
            var paymentCount = payments.Count;
            var firstPaymentDate = payments.Min(p => p.PaymentDate);
            var lastPaymentDate = payments.Max(p => p.PaymentDate);

            return Ok(
                new
                {
                    EmployeeId = employeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    Month = month,
                    Year = year,
                    PaymentCount = paymentCount, // Oy ichida nechta to‘lov bo‘lgani
                    TotalPaidAmount = totalPaidAmount, // Umumiy to‘langan summa
                    Period = new
                    {
                        StartOfMonth = startOfMonth,
                        EndOfMonth = endOfMonth,
                        FirstPaymentDate = firstPaymentDate,
                        LastPaymentDate = lastPaymentDate,
                    },
                    Payrolls = payrolls.Any() ? payrolls : null, // Agar Payroll bo‘lsa qaytariladi
                    Payments = payments, // Batafsil to‘lovlar ro‘yxati
                }
            );
        } // 1. Xodimning o‘z hisoboti

        [HttpGet("[Action]")]
        [Authorize]
        public async Task<IActionResult> GetPayroll(
            [FromQuery] int? id,
            [FromQuery] int? month,
            [FromQuery] int? year
        )
        {
            int employeeId;

            if (id.HasValue)
            {
                // Faqat admin boshqa xodimni ko‘rishi mumkin
                if (!User.IsInRole("Admin"))
                {
                    return Forbid(
                        "Faqat adminlar boshqa xodimlarning ma'lumotlarini ko'rishlari mumkin."
                    );
                }

                employeeId = id.Value;
            }
            else
            {
                // Token ichidan foydalanuvchi ID olamiz
                employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            }

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                return NotFound("Xodim topilmadi.");

            var schedule = await _context.WorkSchedules.FirstOrDefaultAsync(ws =>
                ws.EmployeeId == employeeId
            );
            if (schedule == null)
                return BadRequest("Ish jadvali topilmadi.");

            var startOfMonth = new DateTime(
                year ?? DateTime.Now.Year,
                month ?? DateTime.Now.Month,
                1
            );
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var attendances = await _context
                .Attendances.Where(a =>
                    a.EmployeeId == employeeId
                    && a.AttendanceDate >= startOfMonth
                    && a.AttendanceDate <= endOfMonth
                    && a.ExitTime.HasValue
                )
                .ToListAsync();

            var groupedByDate = attendances.GroupBy(a => a.AttendanceDate.Date);
            var dailyReport = new List<object>();
            double totalHoursWorked = 0;
            decimal totalAmount = 0;

            foreach (var group in groupedByDate)
            {
                var workStart = group.Key + schedule.WorkStartTime;
                var workEnd = group.Key + schedule.WorkEndTime;
                double dailyHoursWorked = 0;
                var dailyEntries = new List<object>();

                foreach (var attendance in group)
                {
                    var entry = attendance.EntryTime > workStart ? attendance.EntryTime : workStart;
                    var exit =
                        attendance.ExitTime.Value < workEnd ? attendance.ExitTime.Value : workEnd;

                    if (exit > entry)
                    {
                        var hoursWorked = (exit - entry).TotalHours;
                        dailyHoursWorked += hoursWorked;

                        dailyEntries.Add(
                            new
                            {
                                EntryTime = entry,
                                ExitTime = exit,
                                HoursWorked = hoursWorked,
                            }
                        );
                    }
                }

                if (dailyHoursWorked > 0)
                {
                    var dailyAmount = (decimal)dailyHoursWorked * schedule.HourlyRate;
                    dailyReport.Add(
                        new
                        {
                            Date = group.Key,
                            Entries = dailyEntries,
                            TotalDailyHours = dailyHoursWorked,
                            DailyAmount = dailyAmount,
                        }
                    );

                    totalHoursWorked += dailyHoursWorked;
                    totalAmount += dailyAmount;
                }
            }

            return Ok(
                new
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeName = $"{employee.FirstName} {employee.LastName}",
                    DailyReport = dailyReport,
                    TotalHoursWorked = totalHoursWorked,
                    TotalAmount = totalAmount,
                    Wallet = employee.Wallet,
                }
            );
        }

        // General Report (avvalgi yangilangan versiya)
        [HttpGet("general-report")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGeneralReport(
            [FromQuery] int? month,
            [FromQuery] int? year
        )
        {
            var startOfMonth = new DateTime(
                year ?? DateTime.Now.Year,
                month ?? DateTime.Now.Month,
                1
            );
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var employees = await _context.Employees.ToListAsync();
            var schedules = await _context.WorkSchedules.ToListAsync();
            var attendances = await _context
                .Attendances.Where(a =>
                    a.AttendanceDate >= startOfMonth
                    && a.AttendanceDate <= endOfMonth
                    && a.ExitTime.HasValue
                )
                .ToListAsync();

            var employeeReports = new List<object>();

            foreach (var employee in employees)
            {
                var schedule = schedules.FirstOrDefault(ws => ws.EmployeeId == employee.EmployeeId);
                if (schedule == null)
                    continue;

                var employeeAttendances = attendances
                    .Where(a => a.EmployeeId == employee.EmployeeId)
                    .GroupBy(a => a.AttendanceDate.Date);
                double totalHoursWorked = 0;
                decimal totalAmount = 0;

                foreach (var group in employeeAttendances)
                {
                    var workStart = group.Key + schedule.WorkStartTime;
                    var workEnd = group.Key + schedule.WorkEndTime;
                    double dailyHoursWorked = 0;

                    foreach (var attendance in group)
                    {
                        var entry =
                            attendance.EntryTime > workStart ? attendance.EntryTime : workStart;
                        var exit =
                            attendance.ExitTime.Value < workEnd
                                ? attendance.ExitTime.Value
                                : workEnd;

                        if (exit > entry)
                        {
                            dailyHoursWorked += (exit - entry).TotalHours;
                        }
                    }

                    totalHoursWorked += dailyHoursWorked;
                    totalAmount += (decimal)dailyHoursWorked * schedule.HourlyRate;
                }

                employeeReports.Add(
                    new
                    {
                        EmployeeId = employee.EmployeeId,
                        EmployeeAvatar = employee.ProfileImagePath,
                        EmployeeName = $"{employee.FirstName} {employee.LastName}",
                        EmployeeEmail = employee.Email,
                        TotalHoursWorked = totalHoursWorked,
                        TotalAmount = totalAmount,
                        Wallet = employee.Wallet,
                    }
                );
            }

            return Ok(new { TotalEmployees = employees.Count, EmployeeReports = employeeReports });
        }
    }

    // DTO modellar
    public class PayPayrollDto
    {
        public int EmployeeId { get; set; }
        public decimal PaidAmount { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
    }
}
