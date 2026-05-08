using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.Infrastructure.Data;

/// <summary>
/// Extension methods for configuring DainnUser database services.
/// </summary>
public static class DatabaseServiceExtensions
{
    /// <summary>
    /// Adds DainnUser database context with multi-provider support.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDainnUserDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["DainnUser:Database:Provider"] ?? "SqlServer";
        var connectionString = configuration["DainnUser:Database:ConnectionString"]
            ?? throw new InvalidOperationException("Database connection string is not configured.");

        services.AddDbContext<DainnUserDbContext>(options =>
        {
            switch (provider)
            {
                case "SqlServer":
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(DainnUserDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
                    break;

                case "PostgreSQL":
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(DainnUserDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null);
                    });
                    break;

                case "MySQL":
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                    {
                        mySqlOptions.MigrationsAssembly(typeof(DainnUserDbContext).Assembly.FullName);
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    });
                    break;

                case "SQLite":
                    options.UseSqlite(connectionString, sqliteOptions =>
                    {
                        sqliteOptions.MigrationsAssembly(typeof(DainnUserDbContext).Assembly.FullName);
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported database provider: {provider}");
            }

            // Enable sensitive data logging in development
            var enableSensitiveDataLogging = configuration["DainnUser:Database:EnableSensitiveDataLogging"];
            if (bool.TryParse(enableSensitiveDataLogging, out var sensitiveDataLogging) && sensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            // Enable detailed errors in development
            var enableDetailedErrors = configuration["DainnUser:Database:EnableDetailedErrors"];
            if (bool.TryParse(enableDetailedErrors, out var detailedErrors) && detailedErrors)
            {
                options.EnableDetailedErrors();
            }
        });

        return services;
    }
}
