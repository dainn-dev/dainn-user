using DainnUser.Core.Enums;

namespace DainnUser.Core.Entities;

/// <summary>
/// Represents a token associated with a user (refresh tokens, verification tokens, etc.).
/// </summary>
public class UserToken
{
    /// <summary>
    /// Gets or sets the unique identifier for the token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the contact identifier for contact verification tokens.
    /// </summary>
    public Guid? ContactId { get; set; }

    /// <summary>
    /// Gets or sets the type of token.
    /// </summary>
    public TokenType TokenType { get; set; }

    /// <summary>
    /// Gets or sets the token value.
    /// </summary>
    public string TokenValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the token has been used.
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the token was used (if applicable).
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the token was revoked (if applicable).
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this token.
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Gets or sets the contact associated with this token.
    /// </summary>
    public UserContact? Contact { get; set; }
}
