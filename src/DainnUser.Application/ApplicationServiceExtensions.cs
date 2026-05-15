using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.TryAddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        // Register application services
        services.TryAddScoped<IAuthenticationService, AuthenticationService>();
        services.TryAddScoped<ITwoFactorService, TwoFactorService>();
        services.TryAddScoped<IRoleService, RoleService>();
        services.TryAddScoped<IProfileService, ProfileService>();
        services.TryAddScoped<ISessionService, SessionService>();
        services.TryAddScoped<ILoginHistoryService, LoginHistoryService>();
        services.TryAddScoped<IActivityService, ActivityService>();
        services.TryAddScoped<IUserManagementService, UserManagementService>();
        services.TryAddScoped<IAddressService, AddressService>();
        services.TryAddScoped<IContactService, ContactService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IContactVerificationSender, EmailContactVerificationSender>());

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(ISocialLoginService)))
        {
            services.AddHttpClient<ISocialLoginService, SocialLoginService>();
        }

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IRecaptchaService)))
        {
            services.AddHttpClient<IRecaptchaService, RecaptchaService>();
        }

        services.TryAddScoped<IGenericOidcService, GenericOidcService>();

        return services;
    }
}
