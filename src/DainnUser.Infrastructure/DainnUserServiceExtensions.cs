using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.Infrastructure;

/// <summary>
/// Extension methods for configuring all DainnUser services.
/// </summary>
public static class DainnUserServiceExtensions
{
    /// <summary>
    /// Adds all DainnUser services to the service collection with default configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDainnUser(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return AddDainnUser(services, configuration, options => { });
    }

    /// <summary>
    /// Adds all DainnUser services to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configureOptions">Action to configure DainnUser options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDainnUser(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DainnUserOptions> configureOptions)
    {
        // Validate configuration
        ValidateConfiguration(configuration);

        // Configure options
        var options = new DainnUserOptions();
        configureOptions(options);
        services.AddSingleton(options);

        // Add database context
        services.AddDainnUserDbContext(configuration);

        // Add repositories
        services.AddDainnUserRepositories();

        // Add application services (using reflection to avoid circular dependency)
        var applicationType = Type.GetType("DainnUser.Application.ApplicationServiceExtensions, DainnUser.Application");
        if (applicationType != null)
        {
            var method = applicationType.GetMethod("AddDainnUserApplication");
            if (method != null)
            {
                method.Invoke(null, new object[] { services });
            }
        }

        // Add infrastructure services
        services.AddDainnUserInfrastructure(configuration);

        return services;
    }

    /// <summary>
    /// Validates required configuration settings.
    /// </summary>
    /// <param name="configuration">The configuration to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when required configuration is missing.</exception>
    private static void ValidateConfiguration(IConfiguration configuration)
    {
        // Validate database configuration
        var connectionString = configuration["DainnUser:Database:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DainnUser database connection string is not configured. " +
                "Please set 'DainnUser:Database:ConnectionString' in your configuration.");
        }

        var provider = configuration["DainnUser:Database:Provider"];
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException(
                "DainnUser database provider is not configured. " +
                "Please set 'DainnUser:Database:Provider' in your configuration. " +
                "Supported providers: SqlServer, PostgreSQL, MySQL, SQLite");
        }

        var supportedProviders = new[] { "SqlServer", "PostgreSQL", "MySQL", "SQLite" };
        if (!supportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Unsupported database provider: {provider}. " +
                $"Supported providers: {string.Join(", ", supportedProviders)}");
        }

        // Validate email configuration
        var smtpHost = configuration["DainnUser:Email:SmtpHost"];
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            throw new InvalidOperationException(
                "DainnUser email SMTP host is not configured. " +
                "Please set 'DainnUser:Email:SmtpHost' in your configuration.");
        }

        var smtpPort = configuration["DainnUser:Email:SmtpPort"];
        if (string.IsNullOrWhiteSpace(smtpPort) || !int.TryParse(smtpPort, out _))
        {
            throw new InvalidOperationException(
                "DainnUser email SMTP port is not configured or invalid. " +
                "Please set 'DainnUser:Email:SmtpPort' in your configuration.");
        }

        var fromEmail = configuration["DainnUser:Email:FromEmail"];
        if (string.IsNullOrWhiteSpace(fromEmail))
        {
            throw new InvalidOperationException(
                "DainnUser email from address is not configured. " +
                "Please set 'DainnUser:Email:FromEmail' in your configuration.");
        }

        // Validate JWT configuration
        var jwtSecret = configuration["DainnUser:Jwt:Secret"];
        if (string.IsNullOrWhiteSpace(jwtSecret))
        {
            throw new InvalidOperationException(
                "DainnUser JWT secret is not configured. " +
                "Please set 'DainnUser:Jwt:Secret' in your configuration. " +
                "The secret must be at least 32 characters (256 bits).");
        }

        if (System.Text.Encoding.UTF8.GetByteCount(jwtSecret) < 32)
        {
            throw new InvalidOperationException(
                "DainnUser JWT secret is too short. It must be at least 32 bytes (256 bits) for HMAC-SHA256.");
        }
    }
}
