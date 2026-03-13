using AutoMapper;
using HRsystem.Models;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Employee to EmployeeDto mapping
        CreateMap<Employee, EmployeeDto>()
            .ForMember(
                dest => dest.DepartmentName,
                opt =>
                    opt.MapFrom(src =>
                        src.Department != null ? src.Department.DepartmentName : null
                    )
            );

        // EmployeeRegisterDto to Employee mapping
        CreateMap<EmployeeRegisterDto, Employee>()
            .ForMember(
                dest => dest.PasswordHash,
                opt => opt.MapFrom(src => BCrypt.Net.BCrypt.HashPassword(src.Password))
            )
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role ?? "Employee"));

        // Employee to Employee (for updates)
        CreateMap<Employee, Employee>();
        CreateMap<Employee, EmployeeNameDto>().ReverseMap();
    }
}
