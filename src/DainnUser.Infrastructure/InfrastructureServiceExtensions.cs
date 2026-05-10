using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Middleware;
using DainnUser.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.Infrastructure;

/// <summary>
/// Extension methods for configuring infrastructure services.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Adds DainnUser infrastructure services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDainnUserInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure email options
        services.Configure<EmailOptions>(configuration.GetSection("DainnUser:Email"));

        // Configure JWT options
        services.Configure<JwtOptions>(configuration.GetSection("DainnUser:Jwt"));

        // Configure rate limiting options
        services.Configure<RateLimitingOptions>(configuration.GetSection("DainnUser:RateLimiting"));

        // Register infrastructure services
        services.AddScoped<IEmailService, EmailService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<RateLimiterRegistry>();

        return services;
    }
}
