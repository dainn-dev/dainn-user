using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
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

        // Register email service
        services.AddScoped<IEmailService, EmailService>();

        return services;
    }
}
