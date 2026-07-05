using CatalogService.BLL.Interfaces;
using CatalogService.DTOs.Category;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _service;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService service, ILogger<CategoryController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("Request to fetch all categories.");
            try
            {
                var categories = await _service.GetAllAsync();
                _logger.LogInformation("Returned {Count} categories.", categories.Count);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories.");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "שגיאה בשליפת הקטגוריות", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Add(CategoryCreateDTO dto)
        {
            _logger.LogInformation("Request to add category: {CategoryName}", dto.Name);

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _service.AddAsync(dto);
                _logger.LogInformation("Category '{CategoryName}' added.", dto.Name);
                return Ok(new { message = "הקטגוריה נוספה בהצלחה" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Conflict adding category: {Message}", ex.Message);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding category '{CategoryName}'.", dto.Name);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "שגיאה בתהליך ההוספה", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Update(int id, CategoryCreateDTO dto)
        {
            _logger.LogInformation("Request to update category ID: {Id} to Name: {NewName}", id, dto.Name);
            try
            {
                await _service.UpdateAsync(id, dto);
                _logger.LogInformation("Category ID {Id} updated.", id);
                return Ok(new { message = "הקטגוריה עודכנה בהצלחה" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Update failed: Category ID {Id} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Update name conflict: {NewName}", dto.Name);
                return Conflict(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category ID {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "שגיאה בתהליך העדכון", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("Request to delete category ID: {Id}", id);
            try
            {
                await _service.DeleteAsync(id);
                _logger.LogInformation("Category ID {Id} deleted.", id);
                return Ok(new { message = "הקטגוריה נמחקה בהצלחה" });
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Delete failed: Category ID {Id} not found.", id);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category ID {Id}.", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "שגיאה בתהליך המחיקה", error = ex.Message });
            }
        }
    }
}
