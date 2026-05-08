using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.Infrastructure.Data;

/// <summary>
/// Extension methods for configuring repository services.
/// </summary>
public static class RepositoryServiceExtensions
{
    /// <summary>
    /// Adds DainnUser repository services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDainnUserRepositories(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
