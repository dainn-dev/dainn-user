namespace DainnUser.Core.Enums;

/// <summary>
/// Represents the status of a user account.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// User account is pending email verification.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// User account is active and can access the system.
    /// </summary>
    Active = 1,

    /// <summary>
    /// User account is temporarily suspended.
    /// </summary>
    Suspended = 2,

    /// <summary>
    /// User account is locked due to security reasons (e.g., too many failed login attempts).
    /// </summary>
    Locked = 3,

    /// <summary>
    /// User account is permanently deactivated.
    /// </summary>
    Deactivated = 4
}
