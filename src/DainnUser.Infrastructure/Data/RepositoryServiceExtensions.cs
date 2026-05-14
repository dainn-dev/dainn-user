using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<ILoginHistoryRepository, LoginHistoryRepository>();
        services.TryAddScoped<ISessionRepository, SessionRepository>();
        services.TryAddScoped<IActivityLogRepository, ActivityLogRepository>();
        services.TryAddScoped<IAddressRepository, AddressRepository>();
        services.TryAddScoped<IContactRepository, ContactRepository>();
        services.TryAddScoped<IUserTokenRepository, UserTokenRepository>();

        // Register Unit of Work
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
