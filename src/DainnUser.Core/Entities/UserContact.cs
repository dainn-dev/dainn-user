namespace DainnUser.Core.Entities;

/// <summary>
/// Represents contact information associated with a user.
/// </summary>
public class UserContact
{
    /// <summary>
    /// Gets or sets the unique identifier for the contact.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the contact type (e.g., "Phone", "Email", "Skype", "Telegram").
    /// </summary>
    public string ContactType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the contact value (e.g., phone number, email address, username).
    /// </summary>
    public string ContactValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this contact has been verified.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is the primary contact of this type.
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the contact was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the contact was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this contact.
    /// </summary>
    public User User { get; set; } = null!;
}
