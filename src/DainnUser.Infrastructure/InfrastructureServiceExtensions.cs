using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Middleware;
using DainnUser.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // Configure storage options
        services.Configure<StorageOptions>(configuration.GetSection("DainnUser:Storage"));

        // Register email provider
        RegisterEmailProvider(services, configuration);

        // Register storage provider
        RegisterStorageProvider(services, configuration);

        // Register infrastructure services
        services.TryAddScoped<IEmailService, EmailService>();
        services.TryAddSingleton<IJwtTokenService, JwtTokenService>();
        services.TryAddSingleton<RateLimiterRegistry>();
        services.TryAddScoped<IAvatarService, AvatarService>();

        return services;
    }

    private static void RegisterEmailProvider(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DainnUser:Email:Provider"] ?? "Smtp";

        switch (provider.ToLowerInvariant())
        {
            case "sendgrid":
                services.TryAddScoped<IEmailProvider, SendGridEmailProvider>();
                break;
            case "awsses":
                services.TryAddScoped<IEmailProvider, AwsSesEmailProvider>();
                break;
            default:
                services.TryAddScoped<IEmailProvider, SmtpEmailProvider>();
                break;
        }
    }

    private static void RegisterStorageProvider(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DainnUser:Storage:Provider"] ?? "Local";

        switch (provider.ToLowerInvariant())
        {
            case "azure":
                services.TryAddScoped<IStorageService, AzureBlobStorageService>();
                break;
            case "awss3":
                if (!services.Any(descriptor => descriptor.ServiceType == typeof(IStorageService)))
                {
                    services.AddHttpClient<IStorageService, AwsS3StorageService>();
                }
                break;
            default:
                services.TryAddScoped<IStorageService, LocalFileSystemStorageService>();
                break;
        }
    }
}
