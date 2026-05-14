namespace DainnUser.Core.Models.Contact;

/// <summary>
/// Data transfer object for adding a contact.
/// </summary>
public class AddContactDto
{
    public string ContactType { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
    public bool SetAsPrimary { get; set; }
}
