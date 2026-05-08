namespace DainnUser.Core.Entities;

/// <summary>
/// Represents a claim associated with a user.
/// </summary>
public class UserClaim
{
    /// <summary>
    /// Gets or sets the unique identifier for the claim.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the claim type (e.g., "email", "role", "permission").
    /// </summary>
    public string ClaimType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the claim value.
    /// </summary>
    public string ClaimValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the claim was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this claim.
    /// </summary>
    public User User { get; set; } = null!;
}
