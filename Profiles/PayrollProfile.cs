using AutoMapper;
using HRsystem.Controllers;
using HRsystem.Models;

namespace HRsystem.Profiles
{
    public class PayrollProfile : Profile
    {
        public PayrollProfile()
        {
        //    CreateMap<Payroll, PayrollDto>()
        //         .ForMember(dest => dest.PayrollId, opt => opt.MapFrom(src => src.PayrollId))
        //         .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.EmployeeId))
        //         .ForMember(dest => dest.EmployeeName, opt => opt.MapFrom(src => $"{src.Employee.FirstName} {src.Employee.LastName}"))
        //         .ForMember(dest => dest.PayDate, opt => opt.MapFrom(src => src.PayDate))
        //         .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
        //         .ForMember(dest => dest.TotalHoursWorked, opt => opt.Ignore())
        //         .ForMember(dest => dest.PayableHours, opt => opt.Ignore())
        //         .ForMember(dest => dest.LatePenalty, opt => opt.Ignore())
        //         .ForMember(dest => dest.OvertimeBonus, opt => opt.Ignore());

            CreateMap<PayrollCreateDto, Payroll>()
                .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => src.EmployeeId))
                .ForMember(dest => dest.PayDate, opt => opt.MapFrom(src => src.PayDate))
                .ForMember(dest => dest.Amount, opt => opt.Ignore())
                .ForMember(dest => dest.Employee, opt => opt.Ignore());

        
        }
    }
}