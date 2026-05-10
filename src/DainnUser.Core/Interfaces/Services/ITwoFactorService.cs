using DainnUser.Core.Models.Authentication;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for TOTP-based two-factor authentication operations.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a new TOTP secret and returns the setup info (secret + otpauth URI)
    /// so the user can scan a QR code. Does NOT persist or enable 2FA yet — call
    /// <see cref="EnableTwoFactorAsync"/> after the user confirms the first code.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="userEmail">The user's email (embedded in the otpauth URI label).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Setup info with the raw secret and the otpauth URI.</returns>
    Task<TwoFactorSetupResult> PrepareEnableAsync(Guid userId, string userEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies <paramref name="code"/> against the pending TOTP secret, then enables 2FA
    /// and generates 10 single-use backup codes which are returned (plain-text, one-time view).
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="code">The 6-digit TOTP code from the authenticator app.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The 10 plain-text backup codes to show once to the user.</returns>
    /// <exception cref="InvalidOperationException">2FA is already enabled or no pending setup found.</exception>
    /// <exception cref="DainnUser.Core.Exceptions.InvalidTwoFactorCodeException">Code is wrong or expired.</exception>
    Task<IReadOnlyList<string>> EnableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables 2FA for the user, removes the secret, and revokes all backup codes and remember-device tokens.
    /// Requires the user to confirm with a valid TOTP code or backup code first.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="code">A valid TOTP code or backup code for confirmation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="DainnUser.Core.Exceptions.InvalidTwoFactorCodeException">Confirmation code is wrong.</exception>
    Task DisableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a TOTP code (or backup code) as part of the login challenge flow.
    /// Returns a remember-device token when <paramref name="rememberDevice"/> is true.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="code">The 6-digit TOTP code or an 8-character backup code.</param>
    /// <param name="rememberDevice">When true, a long-lived device token is issued.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A plain-text remember-device token when <paramref name="rememberDevice"/> is true; null otherwise.
    /// </returns>
    /// <exception cref="DainnUser.Core.Exceptions.InvalidTwoFactorCodeException">Code is wrong or expired.</exception>
    Task<string?> VerifyTwoFactorCodeAsync(Guid userId, string code, bool rememberDevice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a previously issued remember-device token is still valid for the user.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="deviceToken">The plain-text remember-device token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True when the token is valid and the 2FA challenge should be skipped.</returns>
    Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Regenerates the user's 10 backup codes, revoking all existing ones.
    /// Requires a valid TOTP code for confirmation.
    /// </summary>
    /// <param name="userId">The user's ID.</param>
    /// <param name="code">A valid TOTP code for confirmation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The 10 new plain-text backup codes (one-time view).</returns>
    /// <exception cref="DainnUser.Core.Exceptions.InvalidTwoFactorCodeException">TOTP code is wrong.</exception>
    Task<IReadOnlyList<string>> RegenerateBackupCodesAsync(Guid userId, string code, CancellationToken cancellationToken = default);
}
