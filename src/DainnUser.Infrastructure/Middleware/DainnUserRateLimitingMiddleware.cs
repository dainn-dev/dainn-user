using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.RateLimiting;
using DainnUser.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DainnUser.Infrastructure.Middleware;

/// <summary>
/// Middleware that enforces per-endpoint rate limit rules.
/// Active when both <see cref="DainnUserOptions.EnableRateLimiting"/> and
/// <see cref="RateLimitingOptions.Enabled"/> are true and at least one rule is configured.
/// </summary>
public class DainnUserRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RateLimiterRegistry _registry;
    private readonly DainnUserOptions _dainnUserOptions;
    private readonly ILogger<DainnUserRateLimitingMiddleware> _logger;

    /// <summary>
    /// Initializes the middleware.
    /// </summary>
    public DainnUserRateLimitingMiddleware(
        RequestDelegate next,
        RateLimiterRegistry registry,
        DainnUserOptions dainnUserOptions,
        ILogger<DainnUserRateLimitingMiddleware> logger)
    {
        _next = next;
        _registry = registry;
        _dainnUserOptions = dainnUserOptions;
        _logger = logger;
    }

    /// <summary>
    /// Processes a request through the rate limit pipeline.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!_dainnUserOptions.EnableRateLimiting || !_registry.IsEnabled)
        {
            await _next(context);
            return;
        }

        var rule = _registry.Resolve(context.Request.Path.Value);
        if (rule is null)
        {
            await _next(context);
            return;
        }

        var ipAddress = GetClientIp(context);
        if (_registry.IsWhitelisted(ipAddress))
        {
            await _next(context);
            return;
        }

        var partitionKey = BuildPartitionKey(context, rule.Rule.Mode, ipAddress);
        using var lease = await rule.Limiter.AcquireAsync(partitionKey, 1, context.RequestAborted);

        if (lease.IsAcquired)
        {
            await _next(context);
            return;
        }

        await WriteTooManyRequestsAsync(context, rule, lease);
    }

    private static string BuildPartitionKey(HttpContext context, RateLimitMode mode, string? ip)
    {
        var userId = ResolveUserId(context.User);
        var safeIp = string.IsNullOrWhiteSpace(ip) ? "unknown" : ip;

        return mode switch
        {
            RateLimitMode.PerUser => userId is null ? $"ip:{safeIp}" : $"user:{userId}",
            RateLimitMode.PerIpAndUser => $"ip:{safeIp}|user:{userId ?? "anon"}",
            _ => $"ip:{safeIp}"
        };
    }

    private static string? ResolveUserId(ClaimsPrincipal? principal)
    {
        if (principal?.Identity is null || !principal.Identity.IsAuthenticated)
        {
            return null;
        }

        return principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
               ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Resolves the client IP, honoring <c>X-Forwarded-For</c> when present
    /// (consumers behind a proxy should also wire <c>UseForwardedHeaders</c>).
    /// </summary>
    internal static string? GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString();
    }

    private async Task WriteTooManyRequestsAsync(
        HttpContext context,
        RateLimiterRegistry.RuleEntry rule,
        RateLimitLease lease)
    {
        var retryAfter = ResolveRetryAfter(rule, lease);
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers["Retry-After"] = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        context.Response.ContentType = "application/json; charset=utf-8";

        _logger.LogWarning(
            "Rate limit exceeded for path {Path} (rule={Rule}, mode={Mode})",
            context.Request.Path, rule.Rule.Endpoint, rule.Rule.Mode);

        await context.Response.WriteAsync(
            "{\"success\":false,\"data\":null,\"message\":\"Too many requests. Please retry later.\",\"errors\":null}");
    }

    private static TimeSpan ResolveRetryAfter(RateLimiterRegistry.RuleEntry rule, RateLimitLease lease)
    {
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var hint))
        {
            return hint;
        }
        // Fallback: report a worst-case retry equal to one window segment.
        var segments = Math.Max(1, rule.Rule.SegmentsPerWindow);
        return TimeSpan.FromSeconds(Math.Max(1, rule.Rule.WindowSeconds / segments));
    }
}
