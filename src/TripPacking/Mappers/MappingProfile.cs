using AutoMapper;
using TripPacking.DTOs;
using TripPacking.Entities;

namespace TripPacking.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
        CreateMap<User, UpdateUserDto>().ReverseMap();

        CreateMap<Trip, TripDto>().ReverseMap();
        CreateMap<Trip, CreateTripDto>().ReverseMap();
        CreateMap<Trip, UpdateTripDto>().ReverseMap();

        CreateMap<TripMember, TripMemberDto>().ReverseMap();
        CreateMap<TripMember, CreateTripMemberDto>().ReverseMap();

        CreateMap<PackingCategory, PackingCategoryDto>().ReverseMap();
        CreateMap<PackingCategory, CreatePackingCategoryDto>().ReverseMap();

        CreateMap<PackingItem, PackingItemDto>().ReverseMap();
        CreateMap<PackingItem, CreatePackingItemDto>().ReverseMap();

        CreateMap<PackingTemplate, PackingTemplateDto>().ReverseMap();
        CreateMap<PackingTemplate, CreatePackingTemplateDto>().ReverseMap();

        CreateMap<Invitation, InvitationDto>().ReverseMap();
        CreateMap<Invitation, CreateInvitationDto>().ReverseMap();

        CreateMap<Notification, NotificationDto>().ReverseMap();

        CreateMap<TripStatusHistory, TripStatusHistoryDto>()
            .ForMember(dest => dest.ChangedByUserName, opt => opt.MapFrom(src => src.ChangedByUser.Username));
    }
}
