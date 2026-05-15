using System.Security.Claims;
using DainnUser.Core.Configuration;
using DainnUser.Core.Models.Authentication;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for handling generic OpenID Connect (OIDC) provider authentication.
/// </summary>
public interface IGenericOidcService
{
    /// <summary>
    /// Authenticates a user via generic OIDC provider.
    /// Auto-registers new users if no existing account is found.
    /// </summary>
    /// <param name="providerId">The OIDC provider identifier (e.g., "auth0", "okta").</param>
    /// <param name="claimsPrincipal">The claims principal from OIDC authentication.</param>
    /// <param name="ipAddress">The client IP address.</param>
    /// <param name="userAgent">The client user agent.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login result with JWT tokens and user information.</returns>
    Task<LoginResult> LoginWithOidcAsync(
        string providerId,
        ClaimsPrincipal claimsPrincipal,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an OIDC provider account to an authenticated user.
    /// </summary>
    /// <param name="userId">The user ID to link the provider to.</param>
    /// <param name="providerId">The OIDC provider identifier.</param>
    /// <param name="claimsPrincipal">The claims principal from OIDC authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LinkOidcAccountAsync(
        Guid userId,
        string providerId,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of configured OIDC providers.
    /// </summary>
    /// <returns>Enumerable of configured OIDC provider configurations.</returns>
    IEnumerable<OidcProviderConfig> GetConfiguredProviders();
}
