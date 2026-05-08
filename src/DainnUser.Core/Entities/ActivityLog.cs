using DainnUser.Core.Enums;

namespace DainnUser.Core.Entities;

/// <summary>
/// Represents an activity log entry for audit purposes.
/// </summary>
public class ActivityLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the activity log entry.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the type of activity.
    /// </summary>
    public ActivityType ActivityType { get; set; }

    /// <summary>
    /// Gets or sets a description of the activity.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the IP address from which the activity was performed.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Gets or sets the user agent string from the activity.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the activity (stored as JSON).
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the activity occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this activity log entry.
    /// </summary>
    public User User { get; set; } = null!;
}
