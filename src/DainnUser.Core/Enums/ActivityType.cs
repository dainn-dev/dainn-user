namespace DainnUser.Core.Enums;

/// <summary>
/// Represents the type of user activity being logged.
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// User logged in.
    /// </summary>
    Login = 0,

    /// <summary>
    /// User logged out.
    /// </summary>
    Logout = 1,

    /// <summary>
    /// User registered a new account.
    /// </summary>
    Register = 2,

    /// <summary>
    /// User updated their profile.
    /// </summary>
    ProfileUpdate = 3,

    /// <summary>
    /// User changed their password.
    /// </summary>
    PasswordChange = 4,

    /// <summary>
    /// User requested a password reset.
    /// </summary>
    PasswordReset = 5,

    /// <summary>
    /// User enabled two-factor authentication.
    /// </summary>
    TwoFactorEnabled = 6,

    /// <summary>
    /// User disabled two-factor authentication.
    /// </summary>
    TwoFactorDisabled = 7,

    /// <summary>
    /// User verified their email address.
    /// </summary>
    EmailVerified = 8,

    /// <summary>
    /// User verified their phone number.
    /// </summary>
    PhoneVerified = 9,

    /// <summary>
    /// User account was locked.
    /// </summary>
    AccountLocked = 10,

    /// <summary>
    /// User account was unlocked.
    /// </summary>
    AccountUnlocked = 11,

    /// <summary>
    /// User account was suspended.
    /// </summary>
    AccountSuspended = 12,

    /// <summary>
    /// User account was deactivated.
    /// </summary>
    AccountDeactivated = 13,

    /// <summary>
    /// User linked an external login provider.
    /// </summary>
    ExternalLoginAdded = 14,

    /// <summary>
    /// User removed an external login provider.
    /// </summary>
    ExternalLoginRemoved = 15
}
