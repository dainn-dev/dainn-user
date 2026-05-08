namespace DainnUser.Core.Entities;

/// <summary>
/// Represents the many-to-many relationship between users and roles.
/// </summary>
public class UserRole
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the role identifier.
    /// </summary>
    public Guid RoleId { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the role was assigned to the user.
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this user-role relationship.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the role associated with this user-role relationship.
    /// </summary>
    public Role Role { get; set; } = null!;
}
