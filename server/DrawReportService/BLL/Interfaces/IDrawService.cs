using DrawReportService.DTOs.Report;

namespace DrawReportService.BLL.Interfaces;

public interface IDrawService
{
    Task<DrawingResultDTO> DrawWinnerAsync(int giftId);
}
