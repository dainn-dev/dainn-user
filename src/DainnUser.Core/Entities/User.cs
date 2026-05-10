using DainnUser.Core.Enums;

namespace DainnUser.Core.Entities;

/// <summary>
/// Represents a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the email address of the user.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the username of the user.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed password of the user.
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the email has been verified.
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Gets or sets the phone number of the user.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the phone number has been verified.
    /// </summary>
    public bool PhoneVerified { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether two-factor authentication is enabled.
    /// </summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>
    /// Gets or sets the TOTP authenticator secret for two-factor authentication.
    /// </summary>
    public string? TwoFactorSecret { get; set; }

    /// <summary>
    /// Gets or sets the current status of the user account.
    /// </summary>
    public UserStatus Status { get; set; } = UserStatus.Pending;

    /// <summary>
    /// Gets or sets the number of failed login attempts.
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the account was locked (if applicable).
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the user was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the user was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time of the user's last login.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user's profile information.
    /// </summary>
    public UserProfile? Profile { get; set; }

    /// <summary>
    /// Gets or sets the collection of roles assigned to this user.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>
    /// Gets or sets the collection of claims associated with this user.
    /// </summary>
    public ICollection<UserClaim> Claims { get; set; } = new List<UserClaim>();

    /// <summary>
    /// Gets or sets the collection of external login providers linked to this user.
    /// </summary>
    public ICollection<UserLogin> Logins { get; set; } = new List<UserLogin>();

    /// <summary>
    /// Gets or sets the collection of tokens associated with this user.
    /// </summary>
    public ICollection<UserToken> Tokens { get; set; } = new List<UserToken>();

    /// <summary>
    /// Gets or sets the collection of login history records for this user.
    /// </summary>
    public ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();

    /// <summary>
    /// Gets or sets the collection of active sessions for this user.
    /// </summary>
    public ICollection<UserSession> Sessions { get; set; } = new List<UserSession>();

    /// <summary>
    /// Gets or sets the collection of addresses associated with this user.
    /// </summary>
    public ICollection<UserAddress> Addresses { get; set; } = new List<UserAddress>();

    /// <summary>
    /// Gets or sets the collection of contact information for this user.
    /// </summary>
    public ICollection<UserContact> Contacts { get; set; } = new List<UserContact>();

    /// <summary>
    /// Gets or sets the collection of activity logs for this user.
    /// </summary>
    public ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();
}
