using DrawReportService.DTOs.Cart;
using DrawReportService.DTOs.Report;

namespace DrawReportService.BLL.Interfaces;

public interface IReportService
{
    Task<List<GiftWinnerReportDTO>> GetWinnersReportAsync();
    Task<RevenueSummaryDTO> GetRevenueSummaryAsync();
    Task<List<PurchaserDetailsDTO>> GetAllPurchasersAsync();
    Task<PurchaserDetailsDTO> GetPurchaserDetailsAsync(int userId);
    Task<GiftPurchasesSummaryDTO> GetPurchasesByGiftIdAsync(int giftId);
    Task<TopGiftsDTO?> FindTopGiftAsync(string criteria);
}
