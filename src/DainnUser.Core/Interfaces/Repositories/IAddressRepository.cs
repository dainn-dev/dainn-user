using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for UserAddress entity.
/// </summary>
public interface IAddressRepository : IRepository<UserAddress>
{
    /// <summary>
    /// Gets all addresses for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of user's addresses.</returns>
    Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The address if found and belongs to user, null otherwise.</returns>
    Task<UserAddress?> GetByUserIdAndAddressIdAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}
