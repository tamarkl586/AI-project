using AutoMapper;
using CatalogService.BLL.Interfaces;
using CatalogService.DAL.Interfaces;
using CatalogService.DTOs.Gift;
using CatalogService.Models;

namespace CatalogService.BLL.Implementations
{
    public class GiftService : IGiftService
    {
        private readonly IGiftDAL _giftDal;
        private readonly IDonorDAL _donorDal;
        private readonly ICategoryDAL _categoryDal;
        private readonly IMapper _mapper;
        private readonly ILogger<GiftService> _logger;

        public GiftService(IGiftDAL giftDal, IDonorDAL donorDal, ICategoryDAL categoryDal,
            IMapper mapper, ILogger<GiftService> logger)
        {
            _giftDal = giftDal;
            _donorDal = donorDal;
            _categoryDal = categoryDal;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<GiftDTO>> GetAllAsync()
        {
            _logger.LogDebug("Retrieving all gifts.");
            var gifts = await _giftDal.GetAllAsync();
            return _mapper.Map<List<GiftDTO>>(gifts);
        }

        public async Task<GiftDTO?> GetByIdAsync(int id)
        {
            var gift = await _giftDal.GetByIdAsync(id);
            if (gift == null)
                throw new KeyNotFoundException("מתנה לא נמצאה במערכת");
            return _mapper.Map<GiftDTO>(gift);
        }

        public async Task AddAsync(CreateGiftDTO dto)
        {
            _logger.LogInformation("Validating new gift: {GiftName}", dto.Name);

            if (await _giftDal.ExistsByNameAsync(dto.Name))
                throw new InvalidOperationException("שם המתנה כבר קיים במערכת");

            if (!dto.DonorId.HasValue || !dto.CategoryId.HasValue)
                throw new InvalidOperationException("Donor and Category are required.");

            var donor = await _donorDal.GetByIdAsync(dto.DonorId.Value)
                ?? throw new KeyNotFoundException("Donor not found");
            var category = await _categoryDal.GetByIdAsync(dto.CategoryId.Value)
                ?? throw new KeyNotFoundException("Category not found");

            var gift = _mapper.Map<Gift>(dto);
            gift.DonorName = donor.Name;
            gift.CategoryName = category.Name;

            await _giftDal.AddAsync(gift);
            _logger.LogInformation("Gift '{GiftName}' added with ID {Id}", gift.Name, gift.Id);
        }

        public async Task UpdateAsync(int id, GiftUpdateDTO dto)
        {
            var existingGift = await _giftDal.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("המתנה לא נמצאה");

            _logger.LogInformation("Updating Gift ID {Id}", id);

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != existingGift.Name)
                if (await _giftDal.ExistsByNameAsync(dto.Name, id))
                    throw new InvalidOperationException("השם החדש כבר תפוס");

            var oldDonorId = existingGift.DonorId;
            var oldCategoryId = existingGift.CategoryId;

            // Apply scalar updates (mapper skips null / empty / ≤ 0 members per profile config)
            _mapper.Map(dto, existingGift);

            // Re-sync denormalized names when the foreign key actually changed
            if (existingGift.DonorId != oldDonorId)
            {
                var donor = await _donorDal.GetByIdAsync(existingGift.DonorId)
                    ?? throw new KeyNotFoundException("Donor not found");
                existingGift.DonorName = donor.Name;
            }

            if (existingGift.CategoryId.HasValue && existingGift.CategoryId != oldCategoryId)
            {
                var category = await _categoryDal.GetByIdAsync(existingGift.CategoryId.Value)
                    ?? throw new KeyNotFoundException("Category not found");
                existingGift.CategoryName = category.Name;
            }

            await _giftDal.UpdateAsync(existingGift);
            _logger.LogInformation("Update persisted for Gift ID {Id}", id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Manager requested deletion of Gift ID {Id}", id);
            await _giftDal.DeleteAsync(id);
        }

        public async Task<List<GiftDTO>> ManagerSearchAsync(string? giftName, string? donorName)
        {
            _logger.LogDebug("Executing manager search. GiftName: {Gift}, DonorName: {Donor}", giftName, donorName);
            var gifts = await _giftDal.SearchAsync(giftName, donorName);
            return _mapper.Map<List<GiftDTO>>(gifts);
        }

        public async Task<List<GiftDTO>> UserSearchAsync(string? categoryName, int? maxPrice)
        {
            var gifts = await _giftDal.UserSearchAsync(categoryName, maxPrice);
            return _mapper.Map<List<GiftDTO>>(gifts);
        }
    }
}
