namespace HRsystem.Models
{
    public class Payroll
    {
        public int PayrollId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime PayDate { get; set; }       // Hisoblangan sana
        public decimal Amount { get; set; }         // Hisoblangan summa
        public bool IsPaid { get; set; }            // To‘langan/to‘lanmagan statusi
        public DateTime? PaidDate { get; set; }     // To‘langan sana (agar to‘langan bo‘lsa)
        public Employee Employee { get; set; }
    }
}