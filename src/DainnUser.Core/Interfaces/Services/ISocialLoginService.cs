using DainnUser.Core.Enums;
using DainnUser.Core.Models.Authentication;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service interface for social login providers (Google, Facebook, GitHub, Microsoft).
/// </summary>
public interface ISocialLoginService
{
    /// <summary>
    /// Authenticates a user via Google OAuth authorization code flow.
    /// Auto-registers new users if no existing account is found.
    /// </summary>
    Task<LoginResult> LoginWithGoogleAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a Google account to an already-authenticated user.
    /// </summary>
    Task LinkGoogleAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlinks an external login provider from a user account.
    /// The user must have at least one other login method (password or another provider).
    /// </summary>
    Task UnlinkProviderAsync(
        Guid userId,
        LoginProvider provider,
        CancellationToken cancellationToken = default);
}
