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
    public class AttendanceController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public AttendanceController(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        [HttpGet("time")]
        public IActionResult GetServerTime()
        {
            return Ok(DateTime.UtcNow.ToString("HH:mm:ss")); // Server vaqti
        }

        // POST: api/attendance/entry (Ishga kirish vaqti)
        [HttpPost("entry")]
        public async Task<IActionResult> RecordEntry([FromBody] AttendanceEntryDto dto)
        {
            if (!ModelState.IsValid || dto.EmployeeId <= 0)
                return BadRequest(new { Message = "EmployeeId noto‘g‘ri kiritildi." });

            // Employee va WorkSchedule ni o'z ichiga olish
            var employee = await _context
                .Employees.Include(e => e.WorkSchedule)
                .FirstOrDefaultAsync(e => e.EmployeeId == dto.EmployeeId);

            if (employee == null)
                return NotFound(new { Message = "Xodim topilmadi." });

            // Ish jadvali mavjudligini tekshirish
            if (employee.WorkSchedule == null)
                return BadRequest(new { Message = "Xodimning ish jadvali topilmadi." });

            if (string.IsNullOrEmpty(employee.WorkSchedule.WorkDays))
                return BadRequest(new { Message = "Ish kunlari mavjud emas." });

            if (employee.WorkSchedule.WorkEndTime == null)
                return BadRequest(new { Message = "Ishning tugash vaqti ko'rsatilmagan." });

            var now = DateTime.UtcNow;
            var currentDay = now.DayOfWeek.ToString(); // "Monday", "Sunday", "Saturday", ...

            // "Mon", "Tue", "Wed" qisqartmalarini ishlatish
            var workDaysList = employee.WorkSchedule.WorkDays.Split(',');

            // "Mon", "Tue", "Sun" va boshqa qisqartmalarni solishtirish
            if (!workDaysList.Contains(currentDay.Substring(0, 3))) // "Mon", "Tue", "Sun"
                return BadRequest(new { Message = "Bugun xodimning ish kuni emas." });

            var currentTime = now.TimeOfDay; // Hozirgi vaqt (TimeSpan formatida)

            // Ish vaqti tugaganligini tekshirish
            var workEndTime = employee.WorkSchedule.WorkEndTime;
            if (currentTime > workEndTime)
                return BadRequest(new { Message = "Ish vaqti tugagan, kirish qayd etilmaydi." });

            // Avvalgi ochiq kirish borligini tekshirish
            var today = now.Date;
            var lastAttendance = await _context
                .Attendances.Where(a => a.EmployeeId == dto.EmployeeId && a.AttendanceDate == today)
                .OrderByDescending(a => a.EntryTime)
                .FirstOrDefaultAsync();

            if (lastAttendance != null && lastAttendance.ExitTime == null)
                return BadRequest(new { Message = "Avvalgi kirish uchun chiqish qayd etilmagan." });

            var attendance = _mapper.Map<Attendance>(dto);
            attendance.EntryTime = now;
            attendance.AttendanceDate = today;

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(
                new
                {
                    Message = "Kirish vaqti qayd etildi.",
                    AttendanceId = attendance.AttendanceId,
                    EntryTime = attendance.EntryTime,
                }
            );
        }

        // POST: api/attendance/exit (Ishdan chiqish vaqti)
        [HttpPost("exit")]
        public async Task<IActionResult> RecordExit([FromBody] AttendanceEntryDto dto)
        {
            if (!ModelState.IsValid || dto.EmployeeId <= 0)
                return BadRequest(new { Message = "EmployeeId noto‘g‘ri kiritildi." });

            var employee = await _context.Employees.FindAsync(dto.EmployeeId);
            if (employee == null)
                return NotFound(new { Message = "Xodim topilmadi." });

            var schedule = await _context.WorkSchedules.FirstOrDefaultAsync(ws =>
                ws.EmployeeId == dto.EmployeeId
            );
            if (schedule == null)
                return BadRequest(new { Message = "Xodim uchun ish jadvali topilmadi." });

            var today = DateTime.UtcNow.Date;
            var attendance = await _context
                .Attendances.Where(a =>
                    a.EmployeeId == dto.EmployeeId
                    && a.AttendanceDate == today
                    && a.ExitTime == null
                )
                .OrderByDescending(a => a.EntryTime)
                .FirstOrDefaultAsync();

            if (attendance == null)
                return BadRequest(new { Message = "Bugungi kunda ochiq kirish qaydi topilmadi." });

            attendance.ExitTime = DateTime.UtcNow;

            // Ishlagan vaqtni hisoblash
            var workStart = attendance.AttendanceDate.Date + schedule.WorkStartTime;
            var workEnd = attendance.AttendanceDate.Date + schedule.WorkEndTime;
            var entry = attendance.EntryTime > workStart ? attendance.EntryTime : workStart;
            var exit = attendance.ExitTime.Value < workEnd ? attendance.ExitTime.Value : workEnd;

            if (exit > entry)
            {
                var hoursWorked = (exit - entry).TotalHours;
                var earnedAmount = (decimal)hoursWorked * schedule.HourlyRate;

                // Wallet'ga qo‘shish
                employee.Wallet += earnedAmount;

                await _context.SaveChangesAsync();

                var dtoResult = _mapper.Map<AttendanceDto>(attendance);
                return Ok(
                    new
                    {
                        Message = "Chiqish vaqti qayd etildi va pul hisoblandi.",
                        Attendance = dtoResult,
                        HoursWorked = hoursWorked,
                    }
                );
            }
            else
            {
                await _context.SaveChangesAsync();
                var dtoResult = _mapper.Map<AttendanceDto>(attendance);
                return Ok(
                    new
                    {
                        Message = "Chiqish vaqti qayd etildi, ammo ish vaqti hisoblanmadi (jadvaldan tashqari).",
                        Attendance = dtoResult,
                        HoursWorked = 0,
                    }
                );
            }
        }

        // GET: api/attendance/my-attendance (Xodimning o‘z davomati)
        [HttpGet("my-attendance")]
        [Authorize]
        public async Task<IActionResult> GetMyAttendance(
            [FromQuery] int? month = null,
            [FromQuery] int? year = null
        )
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Employee va WorkSchedule ma'lumotlarini olish
            var employee = await _context
                .Employees.Include(e => e.WorkSchedule) // Ish jadvalini qo'shish
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null)
                return NotFound(new { Message = "Xodim topilmadi." });

            var query = _context.Attendances.Where(a => a.EmployeeId == employeeId);

            if (month.HasValue && year.HasValue)
                query = query.Where(a =>
                    a.AttendanceDate.Month == month && a.AttendanceDate.Year == year
                );

            var attendances = await query.ToListAsync();
            var attendanceDtos = _mapper.Map<List<AttendanceDto>>(attendances);

            // WorkDays ni olish
            var workDays = employee.WorkSchedule?.WorkDays;

            var totalHours = attendanceDtos.Sum(a => a.HoursWorked);
            var totalEntries = attendanceDtos.Count(a => a.EntryTime != default);
            var totalCompletedDays = attendanceDtos.Count(a => a.ExitTime.HasValue);

            return Ok(
                new
                {
                    Attendances = attendanceDtos,
                    WorkDays = workDays, // WorkDays ni qo'shish
                    TotalEntries = totalEntries,
                    TotalCompletedDays = totalCompletedDays,
                    TotalHoursWorked = totalHours,
                }
            );
        }

        // GET: api/attendance (Admin uchun barcha davomat)
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllAttendance(
            [FromQuery] int? employeeId = null,
            [FromQuery] int? month = null,
            [FromQuery] int? year = null
        )
        {
            // Include both Employee and WorkSchedule in the query
            var query = _context
                .Attendances.Include(a => a.Employee)
                .ThenInclude(e => e.WorkSchedule)
                .AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            if (month.HasValue && year.HasValue)
                query = query.Where(a =>
                    a.AttendanceDate.Month == month && a.AttendanceDate.Year == year
                );

            var attendances = await query.ToListAsync();
            var attendanceDtos = _mapper.Map<List<AttendanceDto>>(attendances);

            var totalHours = attendanceDtos.Sum(a => a.HoursWorked);
            var totalEntries = attendanceDtos.Count(a => a.EntryTime != default);
            var totalCompletedDays = attendanceDtos.Count(a => a.ExitTime.HasValue);

            // Get WorkDays based on whether we're filtering by employee
            string workDays = null;
            if (employeeId.HasValue && attendances.Any())
            {
                // If filtering by specific employee, get their WorkDays
                workDays = attendances.First().Employee?.WorkSchedule?.WorkDays;
            }
            // Note: If no employeeId is specified, workDays will remain null
            // as we can't return a single WorkDays value for multiple employees

            return Ok(
                new
                {
                    Attendances = attendanceDtos,
                    WorkDays = workDays, // Added WorkDays here
                    TotalEntries = totalEntries,
                    TotalCompletedDays = totalCompletedDays,
                    TotalHoursWorked = totalHours,
                }
            );
        }
    }
}
