namespace DainnUser.Core.Exceptions;

/// <summary>
/// Exception thrown when a contact cannot be found.
/// </summary>
public class ContactNotFoundException : Exception
{
    public ContactNotFoundException(Guid contactId)
        : base($"Contact with ID '{contactId}' was not found.")
    {
    }
}
