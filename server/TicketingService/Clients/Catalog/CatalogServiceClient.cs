using System.Net;
using System.Text.Json;

namespace TicketingService.Clients.Catalog
{
    public sealed class CatalogServiceClient : ICatalogServiceClient
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<CatalogServiceClient> _logger;

        public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<CatalogGiftSnapshot?> GetGiftByIdAsync(int giftId, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync($"/api/gift/{giftId}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Catalog lookup failed for gift {GiftId}. Status {StatusCode}. Payload: {Payload}",
                    giftId,
                    (int)response.StatusCode,
                    payload);
                throw new HttpRequestException($"Catalog lookup failed with status code {(int)response.StatusCode}.");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<CatalogGiftSnapshot>(stream, JsonOptions, cancellationToken);
        }
    }
}