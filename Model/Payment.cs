namespace HRsystem.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }
        public int EmployeeId { get; set; }
        public decimal Amount { get; set; }        // To‘langan summa
        public DateTime PaymentDate { get; set; }  // To‘lov qilingan sana
        public int PayrollId { get; set; }         // Bog‘langan Payroll qaydi (agar mavjud bo‘lsa)
        public string Description { get; set; }    // To‘lov haqida izoh (ixtiyoriy)

        // Bog‘liqliklar
        public Employee Employee { get; set; }
        public Payroll Payroll { get; set; }
    }
}