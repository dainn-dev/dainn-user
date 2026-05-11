namespace DainnUser.Core.Models.Profile;

/// <summary>
/// Data transfer object for updating a user profile.
/// </summary>
public class UpdateProfileDto
{
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
}