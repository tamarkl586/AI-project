using DrawReportService.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DrawReportService.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "manager")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly IDrawService _drawService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, IDrawService drawService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _drawService = drawService;
        _logger = logger;
    }

    [HttpGet("winners")]
    public async Task<IActionResult> GetWinnersReport()
    {
        _logger.LogInformation("Request received to generate winners report.");
        try
        {
            var report = await _reportService.GetWinnersReportAsync();
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while generating winners report.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "שגיאה בהפקת דוח זוכים" });
        }
    }

    [HttpGet("revenue-summary")]
    public async Task<IActionResult> GetRevenueSummary()
    {
        _logger.LogInformation("Request received to fetch revenue summary.");
        try
        {
            var summary = await _reportService.GetRevenueSummaryAsync();
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate revenue summary.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "שגיאה בחישוב נתוני הכנסות" });
        }
    }

    [HttpGet("gift/{giftId}/purchases")]
    public async Task<IActionResult> GetPurchasesByGiftId(int giftId)
    {
        _logger.LogInformation("Request received to view purchases for Gift ID: {GiftId}", giftId);
        try
        {
            var summary = await _reportService.GetPurchasesByGiftIdAsync(giftId);
            return Ok(summary);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchases for Gift ID: {GiftId}", giftId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error while retrieving purchase data." });
        }
    }

    [HttpGet("purchasers")]
    public async Task<IActionResult> GetAllPurchasers()
    {
        try
        {
            var data = await _reportService.GetAllPurchasersAsync();
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching purchasers report");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Internal server error while fetching purchasers report." });
        }
    }

    [HttpGet("purchaser/{userId}")]
    public async Task<IActionResult> GetPurchaserDetails(int userId)
    {
        try
        {
            var details = await _reportService.GetPurchaserDetailsAsync(userId);
            return Ok(details);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchaser details for User ID: {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error while loading purchaser details." });
        }
    }

    [HttpGet("top-gift")]
    public async Task<IActionResult> GetTopGift([FromQuery] string criteria)
    {
        try
        {
            var result = await _reportService.FindTopGiftAsync(criteria);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top gift.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [HttpPost("draw/{giftId}")]
    public async Task<IActionResult> DrawWinner(int giftId)
    {
        try
        {
            var result = await _drawService.DrawWinnerAsync(giftId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Draw failed for Gift ID {GiftId}", giftId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error while drawing winner." });
        }
    }
}
