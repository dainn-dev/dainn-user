using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.Application;

/// <summary>
/// Extension methods for configuring application services.
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Adds DainnUser application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDainnUserApplication(this IServiceCollection services)
    {
        // Register FluentValidation validators
        var assembly = typeof(ApplicationServiceExtensions).Assembly;
        services.AddValidatorsFromAssembly(assembly);

        // Register password hasher
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // Register application services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<IRoleService, RoleService>();

        return services;
    }
}
