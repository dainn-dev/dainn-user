using DainnUser.Infrastructure.Middleware;
using Microsoft.AspNetCore.Builder;

namespace DainnUser.Infrastructure;

/// <summary>
/// Extension methods for configuring DainnUser middleware.
/// </summary>
public static class DainnUserMiddlewareExtensions
{
    /// <summary>
    /// Adds DainnUser middleware to the application pipeline.
    /// Should be invoked AFTER <c>UseAuthentication()</c> so authenticated users are partitioned
    /// correctly by per-user rate limit rules.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseDainnUser(this IApplicationBuilder app)
    {
        app.UseMiddleware<DainnUserRateLimitingMiddleware>();
        return app;
    }
}
