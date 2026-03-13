namespace HRsystem.Models
{
    public class Department
    {
        public int DepartmentId { get; set; } // PK
        public required string DepartmentName { get; set; }
    }
}

// namespace HRsystem.Models
// {
//     public class Department
//     {
//         public int DepartmentId { get; set; }
//         public required string DepartmentName { get; set; }
//         public List<Employee> Employees { get; set; } = new List<Employee>();
//     }
// }
