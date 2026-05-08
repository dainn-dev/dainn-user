namespace DainnUser.Core.Entities;

/// <summary>
/// Represents extended profile information for a user.
/// </summary>
public class UserProfile
{
    /// <summary>
    /// Gets or sets the unique identifier for the profile (same as User.Id).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the first name of the user.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Gets or sets the last name of the user.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gets or sets the display name of the user.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the URL or path to the user's avatar image.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the date of birth of the user.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Gets or sets the gender of the user.
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Gets or sets the preferred language code (e.g., "en", "vi").
    /// </summary>
    public string? Language { get; set; }

    /// <summary>
    /// Gets or sets the preferred timezone (e.g., "Asia/Ho_Chi_Minh").
    /// </summary>
    public string? Timezone { get; set; }

    /// <summary>
    /// Gets or sets the bio or description of the user.
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// Gets or sets the website URL of the user.
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the profile was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the profile was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this profile.
    /// </summary>
    public User User { get; set; } = null!;
}
