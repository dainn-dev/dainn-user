using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for user contacts.
/// </summary>
public interface IContactRepository : IRepository<UserContact>
{
    Task<IEnumerable<UserContact>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserContact?> GetByUserIdAndIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default);
    Task ClearPrimaryForUserAndTypeAsync(Guid userId, string contactType, CancellationToken cancellationToken = default);
    Task<UserContact?> GetPrimaryForUserAndTypeAsync(Guid userId, string contactType, CancellationToken cancellationToken = default);
    Task<bool> UserHasContactsOfTypeAsync(Guid userId, string contactType, CancellationToken cancellationToken = default);
}
