using DainnUser.Core.Enums;

using DainnUser.Core.Enums;

namespace DainnUser.Core.Models.Activity;

/// <summary>
/// Data transfer object for activity log entries.
/// </summary>
public class ActivityLogDto
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
    /// Gets or sets additional metadata about the activity.
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the activity occurred.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
