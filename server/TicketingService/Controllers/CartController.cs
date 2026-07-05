using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TicketingService.BLL.Interfaces;
using TicketingService.DTOs.Cart;
using System.Security.Claims;
using Microsoft.Extensions.Logging;

namespace TicketingService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _service;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService service, ILogger<CartController> logger)
        {
            _service = service;
            _logger = logger;
        }

        private int GetUserId()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthenticated user tried to access personal data.");
                throw new UnauthorizedAccessException("You are not logged in. Please sign in.");
            }

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim == null)
            {
                _logger.LogError("Auth Token is valid but NameIdentifier claim is missing.");
                throw new UnauthorizedAccessException("User identifier was not found in the token.");
            }

            return int.Parse(idClaim.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyCart()
        {
            try
            {
                int userId = GetUserId();
                _logger.LogInformation("User {UserId} requested their cart.", userId);
                var cart = await _service.GetMyCartAsync(userId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve cart.");
                return StatusCode(500, new { message = "Error while loading the cart." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(AddToCartDTO dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid data sent for adding to cart.");
                return BadRequest(ModelState);
            }

            try
            {
                int userId = GetUserId();
                _logger.LogInformation("User {UserId} is adding Gift {GiftId} to cart.", userId, dto.GiftId);
                await _service.AddAsync(userId, dto);
                return Ok(new { message = "Item added to cart successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add item to cart.");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQuantity(int id, [FromBody] int quantity)
        {
            _logger.LogInformation("Updating quantity for CartItem {Id} to {Qty}", id, quantity);
            try
            {
                int userId = GetUserId();
                await _service.UpdateQuantityAsync(id, userId, quantity);
                return Ok(new { message = "Cart quantity updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity for CartItem {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Remove(int id)
        {
            try
            {
                int userId = GetUserId();
                _logger.LogInformation("User {UserId} removing item {Id} from cart.", userId, id);
                await _service.RemoveAsync(id, userId);
                return Ok(new { message = "Item removed from cart successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove item {Id}", id);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase()
        {
            int userId = GetUserId();
            _logger.LogInformation("User {UserId} is processing purchase.", userId);
            try
            {
                await _service.PurchaseAsync(userId);
                _logger.LogInformation("User {UserId} successfully completed purchase.", userId);
                return Ok(new { message = "Purchase completed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Purchase failed for user {UserId}", userId);
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> Clear()
        {
            try
            {
                int userId = GetUserId();
                _logger.LogInformation("User {UserId} is clearing cart.", userId);
                await _service.ClearCartAsync(userId);
                return Ok(new { message = "Cart cleared successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clear cart.");
                return StatusCode(500, new { message = "Error while clearing the cart." });
            }
        }
    }
}