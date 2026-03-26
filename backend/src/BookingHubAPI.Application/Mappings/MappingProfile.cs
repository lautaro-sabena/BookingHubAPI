using AutoMapper;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        
        CreateMap<Company, CompanyResponse>();
        CreateMap<CompanyRequest, Company>();
        
        CreateMap<Domain.Entities.Service, ServiceResponse>();
        CreateMap<ServiceRequest, Domain.Entities.Service>();
        
        CreateMap<Reservation, ReservationResponse>()
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
            .ForMember(dest => dest.ServiceDuration, opt => opt.MapFrom(src => src.Service.DurationMinutes))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));

        CreateMap<Favorite, FavoriteDto>()
            .ForMember(dest => dest.ServiceId, opt => opt.MapFrom(src => src.Service.Id))
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service.Name))
            .ForMember(dest => dest.ServiceDescription, opt => opt.MapFrom(src => src.Service.Description))
            .ForMember(dest => dest.DurationMinutes, opt => opt.MapFrom(src => src.Service.DurationMinutes))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Service.Price))
            .ForMember(dest => dest.CompanyId, opt => opt.MapFrom(src => src.Service.CompanyId))
            .ForMember(dest => dest.CompanyName, opt => opt.MapFrom(src => src.Service.Company.Name));
    }
}
