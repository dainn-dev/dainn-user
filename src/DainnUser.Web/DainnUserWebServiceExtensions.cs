using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.Web;

/// <summary>
/// Extension methods for configuring DainnUser web components.
/// </summary>
public static class DainnUserWebServiceExtensions
{
    /// <summary>
    /// Adds DainnUser web component services to the service collection.
    /// </summary>
    public static IServiceCollection AddDainnUserWeb(this IServiceCollection services)
    {
        return services;
    }
}
