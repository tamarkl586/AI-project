using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebBff.DTOs;

namespace WebBff.Controllers;

[ApiController]
[Route("api/web/orders")]
[Authorize]
public class WebOrdersController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebOrdersController> _logger;

    public WebOrdersController(IHttpClientFactory httpClientFactory, ILogger<WebOrdersController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet("me")]
    public Task<IActionResult> GetMyOrderSummary() => GetOrderSummaryAsync(requireRouteMatch: false);

    [HttpGet("{userId:int}")]
    public Task<IActionResult> GetOrderSummary(int userId) => GetOrderSummaryAsync(requireRouteMatch: true, userId: userId);

    private async Task<IActionResult> GetOrderSummaryAsync(bool requireRouteMatch, int? userId = null)
    {
        try
        {
            var currentUserId = GetCurrentUserId();
            if (requireRouteMatch && userId.HasValue && userId.Value != currentUserId)
            {
                return Forbid();
            }

            var ticketingClient = _httpClientFactory.CreateClient("ticketing");
            var cartItems = await GetJsonAsync<List<TicketingCartItemDto>>(ticketingClient, "/api/cart");
            cartItems ??= [];

            var giftIds = cartItems.Select(item => item.GiftId).Distinct().ToArray();
            var catalogClient = _httpClientFactory.CreateClient("catalog");
            var giftTasks = giftIds.ToDictionary(
                giftId => giftId,
                giftId => GetJsonAsync<CatalogGiftDto>(catalogClient, $"/api/gift/{giftId}")
            );

            await Task.WhenAll(giftTasks.Values);

            var giftsById = giftTasks
                .Where(pair => pair.Value.Result is not null)
                .ToDictionary(pair => pair.Key, pair => pair.Value.Result!);

            var items = cartItems.Select(item => new WebOrderItemDto
            {
                CartItem = item,
                Gift = giftsById.TryGetValue(item.GiftId, out var gift) ? gift : null
            }).ToList();

            var summary = new WebOrderSummaryDto
            {
                UserId = currentUserId,
                RequestedUserId = userId,
                Items = items,
                TotalQuantity = items.Sum(item => item.CartItem.Quantity),
                TotalPrice = items.Sum(item => item.CartItem.TotalPrice),
                RetrievedAtUtc = DateTime.UtcNow
            };

            return Ok(summary);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Authorization failed while building order summary.");
            return Unauthorized(new { message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Downstream HTTP failure while building order summary.");
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Failed to retrieve order summary from downstream services." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while building order summary.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Unexpected error while building order summary." });
        }
    }

    private int GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(claim, out var userId))
        {
            throw new UnauthorizedAccessException("User identifier was not found in the token.");
        }

        return userId;
    }

    private async Task<T?> GetJsonAsync<T>(HttpClient client, string path)
    {
        using var request = CreateAuthorizedRequest(HttpMethod.Get, path);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        var authorizationHeader = Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorizationHeader))
        {
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);
        }

        var userIdHeader = Request.Headers["X-User-Id"].ToString();
        if (!string.IsNullOrWhiteSpace(userIdHeader))
        {
            request.Headers.Add("X-User-Id", userIdHeader);
        }

        var userRoleHeader = Request.Headers["X-User-Role"].ToString();
        if (!string.IsNullOrWhiteSpace(userRoleHeader))
        {
            request.Headers.Add("X-User-Role", userRoleHeader);
        }

        return request;
    }
}
