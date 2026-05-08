using DainnUser.Core.Enums;

namespace DainnUser.Core.Entities;

/// <summary>
/// Represents an external login provider linked to a user account.
/// </summary>
public class UserLogin
{
    /// <summary>
    /// Gets or sets the unique identifier for the user login.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the login provider type.
    /// </summary>
    public LoginProvider Provider { get; set; }

    /// <summary>
    /// Gets or sets the provider-specific key (e.g., Google user ID).
    /// </summary>
    public string ProviderKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name from the provider.
    /// </summary>
    public string? ProviderDisplayName { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the external login was linked.
    /// </summary>
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this external login.
    /// </summary>
    public User User { get; set; } = null!;
}
