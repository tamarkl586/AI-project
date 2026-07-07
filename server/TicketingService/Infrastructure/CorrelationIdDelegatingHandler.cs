namespace TicketingService.Infrastructure;

/// <summary>
/// Propagates X-Correlation-ID from the current HTTP request context
/// to all outbound HttpClient calls (e.g., to CatalogService).
/// </summary>
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = _httpContextAccessor.HttpContext?
            .Request.Headers[CorrelationIdHeader]
            .FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        if (!request.Headers.Contains(CorrelationIdHeader))
        {
            request.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationId);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
