using CatalogService.BLL.Interfaces;
using CatalogService.DTOs.Donor;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "manager")]
    public class DonorController : ControllerBase
    {
        private readonly IDonorService _service;
        private readonly ILogger<DonorController> _logger;

        public DonorController(IDonorService service, ILogger<DonorController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Request received to fetch all donors.");
            try
            {
                var donors = await _service.GetAllAsync();
                _logger.LogInformation("Successfully retrieved {Count} donors.", donors.Count);
                return Ok(donors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all donors.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error retrieving donors list" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("Request received to fetch donor ID: {DonorId}", id);
            try
            {
                var donor = await _service.GetByIdAsync(id);
                return Ok(donor);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Donor ID {DonorId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching donor ID: {DonorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(DonorCreateDTO dto)
        {
            _logger.LogInformation("Attempting to add a new donor: {Name}", dto.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid data for new donor.");
                return BadRequest(ModelState);
            }
            try
            {
                await _service.AddAsync(dto);
                _logger.LogInformation("Donor '{Name}' added successfully.", dto.Name);
                return Created("", new { message = "Donor added successfully" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Failed to add donor '{Name}': {Reason}", dto.Name, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error adding donor '{Name}'", dto.Name);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DonorUpdateDTO dto)
        {
            _logger.LogInformation("Request to update donor ID: {DonorId}", id);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _service.UpdateAsync(id, dto);
                _logger.LogInformation("Donor ID {DonorId} updated successfully.", id);
                return Ok(new { message = "Donor updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Update failed: Donor ID {DonorId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Update conflict for donor ID {DonorId}: {Reason}", id, ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating donor ID: {DonorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Request to delete donor ID: {DonorId}", id);
            try
            {
                await _service.DeleteAsync(id);
                _logger.LogInformation("Donor ID {DonorId} deleted successfully.", id);
                return Ok(new { message = "Donor deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Delete failed: Donor ID {DonorId} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Delete blocked: Donor ID {DonorId} has associated gifts.", id);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting donor ID: {DonorId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? donorName, [FromQuery] string? giftName,
            [FromQuery] string? email)
        {
            _logger.LogInformation("Manager search. Donor: {Donor}, Gift: {Gift}, Email: {Email}",
                donorName, giftName, email);
            try
            {
                var results = await _service.SearchAsync(donorName, giftName, email);
                _logger.LogInformation("Donor search returned {Count} results.", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during donor search.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "שגיאה בביצוע החיפוש" });
            }
        }
    }
}
