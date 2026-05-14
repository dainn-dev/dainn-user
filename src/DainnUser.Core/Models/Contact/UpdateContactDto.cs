namespace DainnUser.Core.Models.Contact;

/// <summary>
/// Data transfer object for updating a contact.
/// </summary>
public class UpdateContactDto
{
    public string? ContactType { get; set; }
    public string? ContactValue { get; set; }
}
