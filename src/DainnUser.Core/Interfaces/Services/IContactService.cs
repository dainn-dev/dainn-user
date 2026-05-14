using DainnUser.Core.Models.Contact;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for managing user contact information.
/// </summary>
public interface IContactService
{
    Task<IReadOnlyList<ContactDto>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ContactDto> GetContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default);
    Task<ContactDto> AddContactAsync(Guid userId, AddContactDto dto, CancellationToken cancellationToken = default);
    Task<ContactDto> UpdateContactAsync(Guid userId, Guid contactId, UpdateContactDto dto, CancellationToken cancellationToken = default);
    Task DeleteContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default);
    Task<ContactDto> SetPrimaryContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default);
    Task SendVerificationCodeAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default);
    Task<ContactDto> VerifyContactAsync(Guid userId, Guid contactId, string code, CancellationToken cancellationToken = default);
}
