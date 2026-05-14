namespace DainnUser.Api.DTOs.Contact;

public class AddContactRequest
{
    public string ContactType { get; set; } = string.Empty;
    public string ContactValue { get; set; } = string.Empty;
    public bool SetAsPrimary { get; set; }
}
