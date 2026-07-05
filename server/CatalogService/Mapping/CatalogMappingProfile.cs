using AutoMapper;
using CatalogService.DTOs.Category;
using CatalogService.DTOs.Donor;
using CatalogService.DTOs.Gift;
using CatalogService.Models;

namespace CatalogService.Mapping
{
    public class CatalogMappingProfile : Profile
    {
        public CatalogMappingProfile()
        {
            // ===== Gifts =====
            // All fields map directly; DonorName/CategoryName are stored in the document
            CreateMap<Gift, GiftDTO>();

            CreateMap<CreateGiftDTO, Gift>()
                .ForMember(dest => dest.DonorName, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryName, opt => opt.Ignore());

            CreateMap<GiftUpdateDTO, Gift>()
                .ForMember(dest => dest.DonorName, opt => opt.Ignore())
                .ForMember(dest => dest.CategoryName, opt => opt.Ignore())
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) =>
                {
                    if (srcMember == null) return false;
                    if (srcMember is string str && (string.IsNullOrWhiteSpace(str) || str == "string")) return false;
                    if (srcMember is int i && i <= 0) return false;
                    return true;
                }));

            // ===== Donors =====
            // Gifts property is populated manually in DonorService (separate query)
            CreateMap<Donor, DonorDTO>()
                .ForMember(dest => dest.Gifts, opt => opt.Ignore());

            CreateMap<DonorCreateDTO, Donor>();

            CreateMap<DonorUpdateDTO, Donor>()
                .ForAllMembers(opt => opt.Condition((src, dest, srcMember) =>
                {
                    if (srcMember == null) return false;
                    if (srcMember is string str && (string.IsNullOrWhiteSpace(str) || str == "string")) return false;
                    return true;
                }));

            // ===== Categories =====
            CreateMap<Category, CategoryDTO>();
            CreateMap<CategoryCreateDTO, Category>();
        }
    }
}
