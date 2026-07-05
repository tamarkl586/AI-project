using AutoMapper;
using CatalogService.BLL.Interfaces;
using CatalogService.DAL.Interfaces;
using CatalogService.DTOs.Donor;
using CatalogService.DTOs.Gift;
using CatalogService.Models;

namespace CatalogService.BLL.Implementations
{
    public class DonorService : IDonorService
    {
        private readonly IDonorDAL _dal;
        private readonly IGiftDAL _giftDal;
        private readonly IMapper _mapper;
        private readonly ILogger<DonorService> _logger;

        public DonorService(IDonorDAL dal, IGiftDAL giftDal, IMapper mapper, ILogger<DonorService> logger)
        {
            _dal = dal;
            _giftDal = giftDal;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<DonorDTO>> GetAllAsync()
        {
            _logger.LogDebug("Fetching all donors.");
            var donors = await _dal.GetAllAsync();
            return await MapWithGiftsAsync(donors);
        }

        public async Task<DonorDTO?> GetByIdAsync(int id)
        {
            var donor = await _dal.GetByIdAsync(id);
            if (donor == null)
            {
                _logger.LogWarning("Donor lookup failed for ID: {DonorId}", id);
                throw new KeyNotFoundException("התורם לא נמצא");
            }

            return (await MapWithGiftsAsync(new[] { donor })).First();
        }

        public async Task AddAsync(DonorCreateDTO dto)
        {
            _logger.LogInformation("Validating new donor: {Name}, Identity: {Identity}", dto.Name, dto.IdentityNumber);

            if (await _dal.ExistsByIdentityNumberAsync(dto.IdentityNumber))
                throw new InvalidOperationException("ת.ז כבר קיימת במערכת");

            if (await _dal.ExistsByNameAsync(dto.Name))
                throw new InvalidOperationException("שם כבר קיים במערכת");

            if (await _dal.ExistsByEmailAsync(dto.Email))
                throw new InvalidOperationException("אימייל כבר קיים במערכת");

            var donor = _mapper.Map<Donor>(dto);
            await _dal.AddAsync(donor);
            _logger.LogInformation("New donor persisted with ID: {DonorId}", donor.Id);
        }

        public async Task UpdateAsync(int id, DonorUpdateDTO dto)
        {
            _logger.LogInformation("Validating updates for donor ID: {DonorId}", id);

            var existingDonor = await _dal.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("התורם לא נמצא");

            if (!string.IsNullOrWhiteSpace(dto.Name) && await _dal.ExistsByNameAsync(dto.Name, id))
                throw new InvalidOperationException("השם החדש כבר קיים במערכת");

            if (!string.IsNullOrWhiteSpace(dto.Email) && await _dal.ExistsByEmailAsync(dto.Email, id))
                throw new InvalidOperationException("האימייל החדש כבר קיים במערכת");

            var oldName = existingDonor.Name;
            _mapper.Map(dto, existingDonor);
            await _dal.UpdateAsync(existingDonor);

            // Propagate name change to denormalized DonorName in all related gifts
            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != oldName)
                await PropagateDonorNameAsync(id, existingDonor.Name);

            _logger.LogInformation("Donor ID {DonorId} updated.", id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Attempting to delete donor ID: {DonorId}", id);

            var existing = await _dal.GetByIdAsync(id)
                ?? throw new KeyNotFoundException("התורם לא נמצא");

            var gifts = await _giftDal.GetByDonorIdAsync(id);
            if (gifts.Any())
                throw new InvalidOperationException("לא ניתן למחוק תורם שיש לו מתנות רשומות");

            await _dal.DeleteAsync(id);
            _logger.LogInformation("Donor ID {DonorId} deleted.", id);
        }

        public async Task<List<DonorDTO>> SearchAsync(string? donorName, string? giftName, string? email)
        {
            _logger.LogDebug("Executing donor search. Donor: {Donor}, Gift: {Gift}, Email: {Email}",
                donorName, giftName, email);

            // When giftName is supplied, first find donor IDs that own matching gifts
            IEnumerable<int>? donorIdsFilter = null;
            if (!string.IsNullOrWhiteSpace(giftName))
            {
                var matchingGifts = await _giftDal.SearchAsync(giftName, null);
                donorIdsFilter = matchingGifts.Select(g => g.DonorId).Distinct().ToList();
            }

            var donors = await _dal.SearchAsync(donorName, email, donorIdsFilter);
            return await MapWithGiftsAsync(donors);
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private async Task<List<DonorDTO>> MapWithGiftsAsync(IEnumerable<Donor> donors)
        {
            var result = new List<DonorDTO>();
            foreach (var donor in donors)
            {
                var dto = _mapper.Map<DonorDTO>(donor);
                var gifts = await _giftDal.GetByDonorIdAsync(donor.Id);
                dto.Gifts = _mapper.Map<List<GiftDTO>>(gifts);
                result.Add(dto);
            }
            return result;
        }

        /// <summary>When a donor's name changes, update the denormalized DonorName on every related gift.</summary>
        private async Task PropagateDonorNameAsync(int donorId, string newName)
        {
            var gifts = await _giftDal.GetByDonorIdAsync(donorId);
            foreach (var gift in gifts)
            {
                gift.DonorName = newName;
                await _giftDal.UpdateAsync(gift);
            }
        }
    }
}
