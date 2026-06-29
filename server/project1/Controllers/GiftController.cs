using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using project1.BLL.Interfaces;
using project1.DTOs.Gift;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace project1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GiftController : ControllerBase
    {
        private readonly IGiftService _service;
        private readonly ILogger<GiftController> _logger;
        private readonly IDistributedCache _cache;

        public GiftController(IGiftService service, ILogger<GiftController> logger, IDistributedCache cache)
        {
            _service = service;
            _logger = logger;
            _cache = cache;
        }

        

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Request received to fetch all gifts.");
            try
            {
                const string cacheKey = "all_gifts_cache";
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (cachedData != null)
                {
                    _logger.LogInformation("Cache hit for all gifts.");
                    var allGifts = JsonSerializer.Deserialize<List<GiftDTO>>(cachedData);
                    return Ok(allGifts);
                }
                _logger.LogInformation("Cache miss for all gifts. Fetching from service.");
                var gifts = await _service.GetAllAsync();
                var serializedData = JsonSerializer.Serialize(gifts);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(300)
                };
                await _cache.SetStringAsync(cacheKey, serializedData, options);
                _logger.LogInformation("Successfully retrieved {Count} gifts and cached.", gifts.Count);
                return Ok(gifts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching all gifts.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving gifts list" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Request received to fetch gift with ID: {GiftId}", id);
            try
            {
                var gift = await _service.GetByIdAsync(id);
                return Ok(gift);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Gift with ID {GiftId} was not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching gift ID: {GiftId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Add([FromBody] CreateGiftDTO dto)
        {
            _logger.LogInformation("Attempting to add a new gift: {Name}", dto.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid data submitted for new gift.");
                return BadRequest(ModelState);
            }
            try
            {
                await _service.AddAsync(dto);
                _logger.LogInformation("Gift '{Name}' added successfully.", dto.Name);
                return Created("", new { message = "המתנה נוספה בהצלחה" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to add gift '{Name}': {Reason}", dto.Name, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while adding gift '{Name}'", dto.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Update(int id, [FromBody] GiftUpdateDTO dto)
        {
            _logger.LogInformation("Request to update gift ID: {GiftId}", id);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _service.UpdateAsync(id, dto);
                _logger.LogInformation("Gift ID {GiftId} updated successfully.", id);
                return Ok(new { message = "המתנה עודכנה בהצלחה" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Update failed: Gift ID {GiftId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Update conflict for gift ID {GiftId}: {Reason}", id, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating gift ID: {GiftId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Request to delete gift ID: {GiftId}", id);
            try
            {
                await _service.DeleteAsync(id);
                _logger.LogInformation("Gift ID {GiftId} deleted successfully.", id);
                return Ok(new { message = "המתנה נמחקה בהצלחה" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Delete failed: Gift ID {GiftId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting gift ID: {GiftId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> UserSearch([FromQuery] string? category, [FromQuery] int? price)
        {
            _logger.LogInformation("Request received for user search. Category: {Category}, MaxPrice: {Price}", category, price);
            try
            {
                var results = await _service.UserSearchAsync(category, price);
                _logger.LogInformation("Successfully retrieved {Count} gifts matching user search.", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user search.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error during search" });
            }
        }

        [HttpGet("manager/search")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> ManagerSearch([FromQuery] string? giftName, [FromQuery] string? donorName, [FromQuery] int? buyersCount)
        {
            _logger.LogInformation("Request received for manager search. Gift: {Gift}, Donor: {Donor}", giftName, donorName);
            try
            {
                var results = await _service.ManagerSearchAsync(giftName, donorName, buyersCount);
                _logger.LogInformation("Successfully retrieved {Count} gifts matching manager search.", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during manager search.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost("{id}/draw")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> DrawWinner(int id)
        {
            _logger.LogInformation("Request to draw winner for gift ID: {GiftId}", id);
            try
            {
                var (winner, emailSent) = await _service.DrawWinnerAsync(id);
                _logger.LogInformation("Winner drawn successfully for gift ID {GiftId}. Winner: {WinnerEmail}, EmailSent: {EmailSent}", id, winner.Email, emailSent);
                return Ok(new { name = winner.Name, email = winner.Email, emailSent });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Draw failed: Gift ID {GiftId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Draw failed for gift ID {GiftId}: {Reason}", id, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing draw for gift ID: {GiftId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }
    }
}