using AutoMapper;
using TicketingService.DTOs.Cart;
using TicketingService.Models;

namespace TicketingService.Mapping
{
    public class TicketingMappingProfile : Profile
    {
        public TicketingMappingProfile()
        {
            CreateMap<Cart, CartItemDTO>()
                .ForMember(dest => dest.GiftId, opt => opt.MapFrom(src => src.GiftID))
                .ForMember(dest => dest.GiftName, opt => opt.MapFrom(src => src.Gift.Name))
                .ForMember(dest => dest.GiftDescription, opt => opt.MapFrom(src => src.Gift.Description))
                .ForMember(dest => dest.GiftPicture, opt => opt.MapFrom(src => src.Gift.Picture))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Gift.Price))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Quantity * src.Gift.Price))
                .ForMember(dest => dest.IsDrawn, opt => opt.MapFrom(src => src.Gift.WinnerId != null));

            CreateMap<AddToCartDTO, Cart>()
                .ForMember(dest => dest.GiftID, opt => opt.MapFrom(src => src.GiftId));
        }
    }
}
