
using AutoMapper;
using HRsystem.Models;

namespace AtirAPI.Profiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<ProductCreateDTO, Product>().ReverseMap();
            CreateMap<Product, ProductDTO>();
            CreateMap<Product, ProductUpdateDTO>().ReverseMap();
        }
    }
}