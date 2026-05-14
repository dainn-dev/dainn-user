using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DainnStripe.Middleware;

/// <summary>
/// Adds low-risk request logging for DainnStripe endpoints without logging payloads, signatures, or API keys.
/// </summary>
public class DainnStripeRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DainnStripeRequestLoggingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeRequestLoggingMiddleware"/> class.
    /// </summary>
    public DainnStripeRequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<DainnStripeRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/stripe", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "DainnStripe request received. Method={Method} Path={Path} TraceId={TraceId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.TraceIdentifier);
        }

        await _next(context);
    }
}
