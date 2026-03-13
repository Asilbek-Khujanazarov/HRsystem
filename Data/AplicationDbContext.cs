using HRsystem.Models;
using Microsoft.EntityFrameworkCore;

namespace HRsystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        // DbSet lar
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<WorkSchedule> WorkSchedules { get; set; }
        public DbSet<JobApplication> JobApplications { get; set; }
        public DbSet<JobApplicationFile> JobApplicationFiles { get; set; }
        public DbSet<Product> Products { get; set; }

        // Model munosabatlari va konfiguratsiyalarni sozlash
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 1. Employee bilan Department o‘rtasidagi 1:ko‘p munosabat
            modelBuilder
                .Entity<Employee>()
                .HasOne(e => e.Department)
                .WithMany()
                .HasForeignKey(e => e.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee uchun indeks va majburiy maydonlar
            modelBuilder.Entity<Employee>().HasIndex(e => e.UserName).IsUnique();
            modelBuilder.Entity<Employee>().Property(e => e.UserName).IsRequired();
            modelBuilder.Entity<Employee>().Property(e => e.PasswordHash).IsRequired();
            modelBuilder.Entity<Employee>().Property(e => e.Role).IsRequired();
            modelBuilder.Entity<Employee>().Property(e => e.FirstName).IsRequired();
            modelBuilder.Entity<Employee>().Property(e => e.LastName).IsRequired();
            modelBuilder.Entity<Employee>().Property(e => e.Position).IsRequired();
            // ProfileImagePath uchun qo‘shimcha konfiguratsiya shart emas, chunki ixtiyoriy
            modelBuilder.Entity<Employee>().Property(e => e.ProfileImagePath).IsRequired(false); // Agar majburiy bo‘lishini xohlasangiz, false ni olib tashlang

            // Department uchun majburiy maydon
            modelBuilder.Entity<Department>().Property(d => d.DepartmentName).IsRequired();

            // 2. Payroll bilan Employee o‘rtasidagi 1:ko‘p munosabat
            modelBuilder
                .Entity<Payroll>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. Attendance bilan Employee o‘rtasidagi 1:ko‘p munosabat
            modelBuilder
                .Entity<Attendance>()
                .HasOne(a => a.Employee)
                .WithMany()
                .HasForeignKey(a => a.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // 5. Employee bilan WorkSchedule o‘rtasidagi 1:1 munosabat
            modelBuilder
                .Entity<Employee>()
                .HasOne(e => e.WorkSchedule)
                .WithOne(ws => ws.Employee)
                .HasForeignKey<WorkSchedule>(ws => ws.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade); // Xodim o‘chirilsa, jadval ham o‘chadi

            // WorkSchedule uchun qo‘shimcha sozlamalar (agar kerak bo‘lsa)
            modelBuilder.Entity<WorkSchedule>().Property(ws => ws.HourlyRate).IsRequired();
            modelBuilder.Entity<WorkSchedule>().Property(ws => ws.WorkStartTime).IsRequired();
            modelBuilder.Entity<WorkSchedule>().Property(ws => ws.WorkEndTime).IsRequired();

            modelBuilder
                .Entity<Payment>()
                .HasOne(p => p.Employee)
                .WithMany()
                .HasForeignKey(p => p.EmployeeId);

            modelBuilder
                .Entity<Payment>()
                .HasOne(p => p.Payroll)
                .WithMany()
                .HasForeignKey(p => p.PayrollId)
                .OnDelete(DeleteBehavior.Restrict); // Payroll o‘chirilganda Payment saqlanib qoladi
        }
    }
}
