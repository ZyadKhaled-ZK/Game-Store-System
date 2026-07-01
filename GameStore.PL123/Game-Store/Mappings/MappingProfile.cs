using AutoMapper;
using GameStore.DAL.Entities;
using GameStore.PL.Models.Auth;
using GameStore.PL.Models.Home;

namespace GameStore.PL.Mappings;

public class MappingProfile : Profile // TECHNOLOGY: AutoMapper Profile - Entity ↔ ViewModel
{
    public MappingProfile()
    {
        // Post → PostViewModel
        CreateMap<Post, PostViewModel>();

        // Review → ReviewDto (flatten User.Username + format CreatedAt)
        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.Username, o => o.MapFrom(s => s.User != null ? s.User.Username : "Anon"))
            .ForMember(d => d.Comment, o => o.MapFrom(s => s.Comment ?? ""))
            .ForMember(d => d.CreatedAt, o => o.MapFrom(s => s.CreatedAt.ToString("yyyy-MM-dd")));
    }
}