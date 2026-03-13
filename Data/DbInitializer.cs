// using HRsystem.Data;
// using HRsystem.Models;
// using Microsoft.EntityFrameworkCore;

// namespace HRsystem.Data
// {
//     public static class DbInitializer
//     {
//         public static void Seed(ApplicationDbContext context)
//         {
//             // Departmentlarni seed qilish
//             if (!context.Departments.Any())
//             {
//                 context.Departments.Add(new Department
//                 {
//                     Name = "IT Department"
//                 });
//                 context.SaveChanges();
//             }

//             // Foydalanuvchilarni seed qilish
//             if (!context.Users.Any())
//             {
//                 context.Users.Add(new User
//                 {
//                     UserName = "admin",
//                     PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
//                     Role = "Admin",
//                     FirstName = "Admin",
//                     LastName = "User",
//                     Email = "admin@example.com",
//                     PhoneNumber = "+998901234567",
//                     DateOfBirth = new DateTime(1990, 1, 1),
//                     HireDate = DateTime.UtcNow,
//                     Position = "Administrator",
//                     City = "Tashkent",
//                     JobTitle = "System Admin",
//                     DepartmentId = 1
//                 });
//                 context.SaveChanges();
//             }
//         }
//     }
// }