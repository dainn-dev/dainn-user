namespace DainnUser.Core.Enums;

/// <summary>
/// Represents the type of token stored for a user.
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Refresh token for JWT authentication.
    /// </summary>
    RefreshToken = 0,

    /// <summary>
    /// Email verification token.
    /// </summary>
    EmailVerification = 1,

    /// <summary>
    /// Password reset token.
    /// </summary>
    PasswordReset = 2,

    /// <summary>
    /// Two-factor authentication token.
    /// </summary>
    TwoFactor = 3,

    /// <summary>
    /// Phone verification token.
    /// </summary>
    PhoneVerification = 4
}
