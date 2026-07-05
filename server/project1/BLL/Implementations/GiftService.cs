using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using project1.BLL.Interfaces;
using project1.DAL.Interfaces;
using project1.DTOs.Gift;
using project1.Models;

namespace project1.BLL.Implementations
{
    public class GiftService : IGiftService
    {
        private readonly IGiftDAL _giftDal;
        private readonly IDonorDAL _donorDal;
        private readonly ICategoryDAL _categoryDal;
        private readonly ICartDAL _cartDal;
        private readonly IMapper _mapper;
        private readonly ILogger<GiftService> _logger;
        private readonly IEmailService _emailService;

        public GiftService(IGiftDAL giftDal, IDonorDAL donorDAL, ICategoryDAL categoryDAL, ICartDAL cartDal, IMapper mapper, ILogger<GiftService> logger, IEmailService emailService)
        {
            _giftDal = giftDal;
            _donorDal = donorDAL;
            _categoryDal = categoryDAL;
            _cartDal = cartDal;
            _mapper = mapper;
            _logger = logger;
            _emailService = emailService;
        }

        

        public async Task<List<GiftDTO>> GetAllAsync()
        {
            _logger.LogDebug("Retrieving all gifts with included Category and Donor info.");
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

            var donor = await _donorDal.GetByIdAsync(dto.DonorId.Value) ?? throw new KeyNotFoundException("Donor not found");
            var category = await _categoryDal.GetByIdAsync(dto.CategoryId.Value) ?? throw new KeyNotFoundException("Category not found");

            var gift = _mapper.Map<Gift>(dto);
            await _giftDal.AddAsync(gift);
            _logger.LogInformation("Gift {GiftName} successfully created with ID {Id}", gift.Name, gift.Id);
        }

        public async Task UpdateAsync(int id, GiftUpdateDTO dto)
        {
            var existingGift = await _giftDal.GetByIdAsync(id) ?? throw new KeyNotFoundException("המתנה לא נמצאה");

            _logger.LogInformation("Updating Gift ID {Id}", id);

            if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != existingGift.Name)
            {
                if (await _giftDal.ExistsByNameAsync(dto.Name, id))
                    throw new InvalidOperationException("השם החדש כבר תפוס");
            }

            _mapper.Map(dto, existingGift);
            await _giftDal.UpdateAsync(existingGift);
            _logger.LogInformation("Update persisted for Gift ID {Id}", id);
        }

        public async Task DeleteAsync(int id)
        {
            _logger.LogInformation("Manager requested deletion of Gift ID {Id}", id);
            await _giftDal.DeleteAsync(id);
        }

        public async Task<List<GiftDTO>> ManagerSearchAsync(string? giftName, string? donorName, int? minBuyers)
        {
            _logger.LogDebug("Executing manager search with filters.");
            IQueryable<Gift> query = _giftDal.GetSearchQuery()
                .AsNoTracking()
                .Include(g => g.Donor)
                .Include(g => g.Category)
                .Include(g => g.Carts)
                .Include(g => g.Winner);

            if (!string.IsNullOrWhiteSpace(giftName))
                query = query.Where(g => g.Name.Contains(giftName));

            if (!string.IsNullOrWhiteSpace(donorName))
                query = query.Where(g => g.Donor.Name.Contains(donorName));

            var results = await query.ToListAsync();

            if (minBuyers.HasValue)
            {
                _logger.LogDebug("Filtering results by minimum buyers: {Min}", minBuyers);
                results = results.Where(g => g.Carts.Where(c => c.IsPurchased).Sum(c => c.Quantity) >= minBuyers.Value).ToList();
            }

            return _mapper.Map<List<GiftDTO>>(results);
        }

        public async Task<List<GiftDTO>> UserSearchAsync(string? categoryName, int? maxPrice)
        {
            IQueryable<Gift> query = _giftDal.GetSearchQuery()
                .AsNoTracking()
                .Include(g => g.Donor)
                .Include(g => g.Category)
                .Include(g => g.Winner);

            if (!string.IsNullOrWhiteSpace(categoryName))
                query = query.Where(g => g.Category != null && g.Category.Name.Contains(categoryName));

            if (maxPrice.HasValue)
                query = query.Where(g => g.Price <= maxPrice.Value);

            var results = await query.ToListAsync();
            return _mapper.Map<List<GiftDTO>>(results);
        }

        public async Task<(User Winner, bool EmailSent)> DrawWinnerAsync(int giftId)
        {
            _logger.LogInformation("Draw process started for Gift ID {Id}", giftId);

            var gift = await _giftDal.GetByIdAsync(giftId) ?? throw new KeyNotFoundException("מתנה לא נמצאה");

            if (gift.WinnerId != null)
                throw new InvalidOperationException("הגרלה כבר בוצעה למתנה זו");

            var purchasedCarts = await _cartDal.GetPurchasedByGiftAsync(giftId);

            if (purchasedCarts == null || !purchasedCarts.Any())
            {
                _logger.LogWarning("Draw aborted for Gift {Id}: No purchases found.", giftId);
                throw new InvalidOperationException("לא ניתן לבצע הגרלה - אין רכישות למתנה זו");
            }

            var pool = purchasedCarts.SelectMany(c => Enumerable.Repeat(c.User, c.Quantity)).ToList();
            var winner = pool[new Random().Next(pool.Count)];

            gift.WinnerId = winner.Id;
            await _giftDal.UpdateAsync(gift);
            _logger.LogInformation("Winner found: {WinnerEmail} for Gift {GiftName}", winner.Email, gift.Name);

            // Send email using the injected IEmailService
            bool emailSent = false;
            try
            {
                var emailBody = $@"
                    <div dir='rtl' style='width: 100%; background-color: #f9f9f9; padding: 50px 0; font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff; padding: 30px; border: 2px solid #d4af37; border-radius: 10px; text-align: center;'>
                            <h1 style='color: #AEC6CF;'>מזל טוב {winner.Name}!</h1>
                            <h2 style='font-family: Arial; color: pink'> אנו שמחים לבשר לך על זכייתך בפרס </h2>
                            <h1 style='font-family: Arial; color: Lavender'>{gift.Name}</h1>
                            <h3 style='font-family: Arial; color: lightgreen'>נציג יצור איתך קשר בהקדם</h3>
                            <hr>
                            <h4 style='font-family: Arial; color: lightblue'>בברכה, הנהלת המכירה הסינית</h4>
                        </div>
                    </div>";

                await _emailService.SendEmailAsync(winner.Email, "מזל טוב! זכית בהגרלה", emailBody);
                emailSent = true;
                _logger.LogInformation("Success: Winning email sent to {Email}", winner.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Winning email failed for {Email}, but winner was saved in DB.", winner.Email);
            }

            return (winner, emailSent);
        }
    }
}