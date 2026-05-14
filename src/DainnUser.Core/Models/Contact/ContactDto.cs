namespace DainnUser.Core.Models.Contact;

/// <summary>
/// Data transfer object for user contact information.
/// </summary>
public class ContactDto
{
    public Guid Id { get; set; }
    public string ContactType { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
