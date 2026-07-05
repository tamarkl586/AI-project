using AutoMapper;
using project1.DTOs.Auth;
using project1.DTOs.Cart;
using project1.DTOs.Category;
using project1.DTOs.Donor;
using project1.DTOs.Gift;
using project1.DTOs.Report;
using project1.Models;

namespace project1.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // ===== Auth & Users =====
            CreateMap<RegisterDTO, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => "user"))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src => src.Phone))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name));

            CreateMap<User, UserDTO>();

            // ===== Gifts =====
            CreateMap<Gift, GiftDTO>()
                .ForMember(dest => dest.DonorName, opt => opt.MapFrom(src => src.Donor.Name))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : "N/A"))
                .ForMember(dest => dest.WinnerName, opt => opt.MapFrom(src => src.Winner != null ? src.Winner.Name : "N/A"))
                .ForMember(dest => dest.WinnerEmail, opt => opt.MapFrom(src => src.Winner != null ? src.Winner.Email : null));

            CreateMap<CreateGiftDTO, Gift>();

            CreateMap<GiftUpdateDTO, Gift>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) =>
                {
                    if (srcMember == null) return false;
                    if (srcMember is string str && (string.IsNullOrWhiteSpace(str) || str == "string")) return false;
                    if (srcMember is int i && i <= 0) return false;
                    return true;
                }));

            // ===== Donors =====
            CreateMap<Donor, DonorDTO>();

            CreateMap<DonorCreateDTO, Donor>();

            CreateMap<DonorUpdateDTO, Donor>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) =>
                {
                    if (srcMember == null) return false;
                    if (srcMember is string str && (string.IsNullOrWhiteSpace(str) || str == "string")) return false;
                    return true;
                }));


            // ===== Cart =====
            CreateMap<Cart, CartItemDTO>()
                .ForMember(dest => dest.GiftName, opt => opt.MapFrom(src => src.Gift.Name))
                .ForMember(dest => dest.GiftDescription, opt => opt.MapFrom(src => src.Gift.Description))
                .ForMember(dest => dest.GiftPicture, opt => opt.MapFrom(src => src.Gift.Picture))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Gift.Price))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Quantity * src.Gift.Price))
                .ForMember(dest => dest.IsDrawn, opt => opt.MapFrom(src => src.Gift.WinnerId != null));

            CreateMap<AddToCartDTO, Cart>()
                .ForMember(dest => dest.GiftID, opt => opt.MapFrom(src => src.GiftId));

            CreateMap<Cart, GiftPurchaseDTO>()
                .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src => src.User.Name))
                .ForMember(dest => dest.BuyerEmail, opt => opt.MapFrom(src => src.User.Email));

            CreateMap<Cart, PurchaserItemDTO>()
                .ForMember(dest => dest.GiftName, opt => opt.MapFrom(src => src.Gift.Name))
                .ForMember(dest => dest.PricePerUnit, opt => opt.MapFrom(src => src.Gift.Price))
                .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.Quantity * src.Gift.Price))
                .ForMember(dest => dest.PurchaseDate, opt => opt.MapFrom(src => src.CreatedAt));

            CreateMap<User, PurchaserDetailsDTO>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email));

            // ===== Categories =====
            CreateMap<Category, CategoryDTO>();

            CreateMap<CategoryCreateDTO, Category>();

            CreateMap<Gift, GiftWinnerReportDTO>()
                .ForMember(dest => dest.GiftName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Picture, opt => opt.MapFrom(src => src.Picture))
                .ForMember(dest => dest.WinnerName, opt => opt.MapFrom(src => src.Winner != null ? src.Winner.Name : "טרם הוגרל זוכה"))
                .ForMember(dest => dest.ContactEmail, opt => opt.MapFrom(src => src.Winner != null ? src.Winner.Email : "N/A"));

            CreateMap<IEnumerable<Cart>, RevenueSummaryDTO>()
                .ForMember(dest => dest.TotalRevenue, opt => opt.MapFrom(src => src.Sum(c => c.Quantity * c.Gift.Price)))
                .ForMember(dest => dest.TotalTicketsSold, opt => opt.MapFrom(src => src.Sum(c => c.Quantity)))
                .ForMember(dest => dest.TotalParticipants, opt => opt.MapFrom(src => src.Select(c => c.UserID).Distinct().Count()));

            CreateMap<IEnumerable<Cart>, GiftPurchasesSummaryDTO>()
                .ForMember(dest => dest.GiftId, opt => opt.MapFrom(src => src.First().GiftID))
                .ForMember(dest => dest.GiftName, opt => opt.MapFrom(src => src.First().Gift.Name))
                .ForMember(dest => dest.Purchasers, opt => opt.MapFrom(src => src))
                .ForMember(dest => dest.TotalTicketsPurchased, opt => opt.MapFrom(src => src.Sum(c => c.Quantity)))
                .ForMember(dest => dest.TotalEarned, opt => opt.MapFrom(src => src.Sum(c => c.Quantity * c.Gift.Price)));

            CreateMap<Gift, TopGiftsDTO>()
                .ForMember(dest => dest.GiftId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.GiftName, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Picture, opt => opt.MapFrom(src => src.Picture))
                .ForMember(dest => dest.WinnerName, opt => opt.MapFrom(src => src.Winner != null ? src.Winner.Name : "N/A"))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.TotalTicketsPurchased, opt => opt.MapFrom(src => src.Carts.Where(c => c.IsPurchased).Sum(c => c.Quantity)))
                .ForMember(dest => dest.TotalEarned, opt => opt.MapFrom(src => src.Carts.Where(c => c.IsPurchased).Sum(c => c.Quantity * c.Gift.Price)));

            CreateMap<TopGiftStatsDTO, TopGiftsDTO>()
                .ForMember(dest => dest.WinnerName, opt => opt.Ignore());
        }
    }
}