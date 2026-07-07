using Serilog.Context;

namespace IdentityService.Middleware;

/// <summary>
/// Reads or generates an X-Correlation-ID header for every HTTP request
/// and enriches the Serilog LogContext so every log line in the request
/// automatically carries the correlation ID.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        // Make it available to downstream code via HttpContext.Items
        context.Items[CorrelationIdHeader] = correlationId;

        // Echo it back in the response
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Push into Serilog's async-local context so every log statement
        // in this request automatically includes the CorrelationId property.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}
