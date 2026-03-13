using AutoMapper;
using HRsystem.Models;

public class JobApplicationProfile : Profile
{
    public JobApplicationProfile()
    {
        CreateMap<JobApplication, JobApplicationDto>();
        CreateMap<JobApplicationFile, JobApplicationFileDto>();
    }
}
