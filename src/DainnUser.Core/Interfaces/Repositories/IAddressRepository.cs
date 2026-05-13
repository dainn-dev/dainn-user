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
    /// Gets a specific address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The address if found and belongs to user, otherwise null.</returns>
    Task<UserAddress?> GetByUserIdAndIdAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the default flag for all addresses of a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default address if found, otherwise null.</returns>
    Task<UserAddress?> GetDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any addresses.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if user has addresses, otherwise false.</returns>
    Task<bool> UserHasAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
}
