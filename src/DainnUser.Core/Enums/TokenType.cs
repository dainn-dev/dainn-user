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
    PhoneVerification = 4,

    /// <summary>
    /// TOTP two-factor authenticator secret (stored encrypted/hashed for the user).
    /// </summary>
    TwoFactorSecret = 5,

    /// <summary>
    /// Single-use 2FA backup code (stored as PBKDF2 hash).
    /// </summary>
    TwoFactorBackupCode = 6,

    /// <summary>
    /// Remember-device token that bypasses 2FA challenge for a trusted device.
    /// </summary>
    TwoFactorRememberDevice = 7,

    /// <summary>
    /// Contact verification token for email, phone, or social contacts.
    /// </summary>
    ContactVerification = 8
}
