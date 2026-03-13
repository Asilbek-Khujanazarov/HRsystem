namespace HRsystem.Models
{
    public class WorkSchedule
    {
        public int WorkScheduleId { get; set; }
        public int EmployeeId { get; set; }
        public TimeSpan WorkStartTime { get; set; } // Kelish vaqti
        public TimeSpan WorkEndTime { get; set; }   // Ketish vaqti
        public string WorkDays { get; set; }        // Haftalik ish kunlari (masalan, "Mon,Tue,Wed")
        public decimal HourlyRate { get; set; }     // Soatlik ish haqi
        public Employee Employee { get; set; }
    }
}