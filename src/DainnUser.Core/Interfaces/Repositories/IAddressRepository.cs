using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for UserAddress entity with specific query methods.
/// </summary>
public interface IAddressRepository : IRepository<UserAddress>
{
    /// <summary>
    /// Gets all addresses for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of addresses for the user.</returns>
    Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an address by its identifier and verifies it belongs to the specified user.
    /// </summary>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The address if found and belongs to the user, otherwise null.</returns>
    Task<UserAddress?> GetByIdAndUserIdAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default address for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default address if one exists, otherwise null.</returns>
    Task<UserAddress?> GetDefaultByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsets the default flag for all addresses belonging to a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task UnsetDefaultAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
}
