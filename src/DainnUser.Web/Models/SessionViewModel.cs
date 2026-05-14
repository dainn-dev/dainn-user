namespace DainnUser.Web.Models;

/// <summary>
/// Presentation model for a user session, used by the <c>SessionList</c> component.
/// </summary>
public class SessionViewModel
{
    public Guid Id { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    /// <summary>When true the row is highlighted and the revoke button is hidden.</summary>
    public bool IsCurrent { get; set; }
}
