using DainnUser.Core.Enums;

namespace DainnUser.Core.Entities;

/// <summary>
/// Represents a login history record for audit purposes.
/// </summary>
public class LoginHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the login history record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the login provider used.
    /// </summary>
    public LoginProvider Provider { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the login attempt was successful.
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the login attempt was made.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the login attempt.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets the failure reason (if login was unsuccessful).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the login attempt occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this login history record.
    /// </summary>
    public User User { get; set; } = null!;
}
