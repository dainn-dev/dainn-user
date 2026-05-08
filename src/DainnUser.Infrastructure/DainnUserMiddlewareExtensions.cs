using Microsoft.AspNetCore.Builder;

namespace DainnUser.Infrastructure;

/// <summary>
/// Extension methods for configuring DainnUser middleware.
/// </summary>
public static class DainnUserMiddlewareExtensions
{
    /// <summary>
    /// Adds DainnUser middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseDainnUser(this IApplicationBuilder app)
    {
        // Currently no middleware needed, but this provides extension point for future features
        // such as:
        // - Custom authentication middleware
        // - Rate limiting middleware
        // - Activity logging middleware
        // - Session management middleware

        return app;
    }
}
