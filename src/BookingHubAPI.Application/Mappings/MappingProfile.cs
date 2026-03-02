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
    }
}
