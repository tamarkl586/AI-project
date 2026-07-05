using CatalogService.BLL.Interfaces;
using CatalogService.DTOs.Gift;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GiftController : ControllerBase
    {
        private const string AllGiftsCacheKey = "all_gifts_cache";
        private const string GiftByIdCacheKeyPrefix = "gift_by_id_";

        private static readonly DistributedCacheEntryOptions AllGiftsCacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        private static readonly DistributedCacheEntryOptions GiftByIdCacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
        };

        private static readonly JsonSerializerOptions CacheSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

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
                var cached = await TryGetFromCacheAsync<List<GiftDTO>>(AllGiftsCacheKey, "all gifts");
                if (cached != null)
                {
                    _logger.LogInformation("Cache hit for all gifts. Returning {Count} items.", cached.Count);
                    return Ok(cached);
                }

                _logger.LogInformation("Cache miss for all gifts. Loading from database.");
                var gifts = await _service.GetAllAsync();
                await SetCacheValueAsync(AllGiftsCacheKey, gifts, AllGiftsCacheOptions);
                _logger.LogInformation("Successfully retrieved {Count} gifts.", gifts.Count);
                return Ok(gifts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all gifts.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving gifts list" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Request received to fetch gift ID: {GiftId}", id);
            try
            {
                var cacheKey = GetGiftByIdCacheKey(id);
                var cached = await TryGetFromCacheAsync<GiftDTO>(cacheKey, $"gift {id}");
                if (cached != null)
                {
                    _logger.LogInformation("Cache hit for gift {GiftId}.", id);
                    return Ok(cached);
                }

                _logger.LogInformation("Cache miss for gift {GiftId}. Loading from database.", id);
                var gift = await _service.GetByIdAsync(id);
                await SetCacheValueAsync(cacheKey, gift, GiftByIdCacheOptions);
                return Ok(gift);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Gift ID {GiftId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching gift ID: {GiftId}", id);
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
                _logger.LogWarning("Invalid data for new gift.");
                return BadRequest(ModelState);
            }
            try
            {
                await _service.AddAsync(dto);
                await InvalidateGiftCachesAsync();
                _logger.LogInformation("Gift '{Name}' added successfully.", dto.Name);
                return Created("", new { message = "המתנה נוספה בהצלחה" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Failed to add gift '{Name}': {Reason}", dto.Name, ex.Message);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Conflict adding gift '{Name}': {Reason}", dto.Name, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding gift '{Name}'", dto.Name);
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
                await InvalidateGiftCachesAsync(id);
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
                await InvalidateGiftCachesAsync(id);
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
            _logger.LogInformation("User search. Category: {Category}, MaxPrice: {Price}", category, price);
            try
            {
                var results = await _service.UserSearchAsync(category, price);
                _logger.LogInformation("User search returned {Count} gifts.", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user search.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error during search" });
            }
        }

        [HttpGet("manager/search")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> ManagerSearch([FromQuery] string? giftName, [FromQuery] string? donorName)
        {
            _logger.LogInformation("Manager search. Gift: {Gift}, Donor: {Donor}", giftName, donorName);
            try
            {
                var results = await _service.ManagerSearchAsync(giftName, donorName);
                _logger.LogInformation("Manager search returned {Count} gifts.", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manager search.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        // ── Cache helpers ─────────────────────────────────────────────────────────

        private async Task<T?> TryGetFromCacheAsync<T>(string cacheKey, string entityName)
        {
            try
            {
                var data = await _cache.GetStringAsync(cacheKey);
                if (string.IsNullOrWhiteSpace(data)) return default;

                var deserialized = JsonSerializer.Deserialize<T>(data, CacheSerializerOptions);
                if (deserialized != null) return deserialized;

                _logger.LogWarning("Cache key '{Key}' for {Entity} had null payload. Removing.", cacheKey, entityName);
                await _cache.RemoveAsync(cacheKey);
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Cache key '{Key}' for {Entity} had invalid JSON. Removing.", cacheKey, entityName);
                await _cache.RemoveAsync(cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache read failed for '{Key}' ({Entity}). Falling back to DB.", cacheKey, entityName);
            }
            return default;
        }

        private async Task SetCacheValueAsync<T>(string cacheKey, T value, DistributedCacheEntryOptions options)
        {
            try
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(value, CacheSerializerOptions), options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache write failed for key '{Key}'. Request will still succeed.", cacheKey);
            }
        }

        private static string GetGiftByIdCacheKey(int id) => $"{GiftByIdCacheKeyPrefix}{id}";

        private async Task InvalidateGiftCachesAsync(int? id = null)
        {
            try
            {
                await _cache.RemoveAsync(AllGiftsCacheKey);
                _logger.LogInformation("Invalidated cache key '{Key}'.", AllGiftsCacheKey);

                if (id.HasValue)
                {
                    var byIdKey = GetGiftByIdCacheKey(id.Value);
                    await _cache.RemoveAsync(byIdKey);
                    _logger.LogInformation("Invalidated cache key '{Key}'.", byIdKey);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cache invalidation failed for key '{Key}'.", AllGiftsCacheKey);
                throw new ApplicationException("Cache invalidation failed for gifts list.", ex);
            }
        }
    }
}
