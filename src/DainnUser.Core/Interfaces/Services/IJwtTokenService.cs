using System.Security.Claims;
using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for generating and validating JWT access tokens and refresh tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT access token for a user.
    /// </summary>
    /// <param name="user">The user the token is issued for.</param>
    /// <param name="roles">The roles assigned to the user, included as claims.</param>
    /// <param name="sessionId">The unique session identifier, included as the "sid" claim.</param>
    /// <returns>The generated access token and its absolute expiration time (UTC).</returns>
    AccessTokenResult GenerateAccessToken(User user, IEnumerable<string> roles, Guid sessionId);

    /// <summary>
    /// Generates a cryptographically random refresh token (URL-safe base64).
    /// </summary>
    /// <returns>The generated refresh token in plain form. The caller is responsible for hashing it before persistence.</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Computes a deterministic hash of a refresh token suitable for storage.
    /// </summary>
    /// <param name="refreshToken">The plain refresh token.</param>
    /// <returns>A base64-encoded SHA-256 hash of the token.</returns>
    string HashRefreshToken(string refreshToken);

    /// <summary>
    /// Validates a JWT access token and returns the associated principal if valid.
    /// </summary>
    /// <param name="accessToken">The token to validate.</param>
    /// <returns>The validated <see cref="ClaimsPrincipal"/>, or <c>null</c> if validation fails.</returns>
    ClaimsPrincipal? ValidateAccessToken(string accessToken);
}

/// <summary>
/// Represents the result of generating an access token.
/// </summary>
/// <param name="Token">The signed JWT access token.</param>
/// <param name="ExpiresAt">The absolute expiration time of the token (UTC).</param>
public record AccessTokenResult(string Token, DateTime ExpiresAt);
