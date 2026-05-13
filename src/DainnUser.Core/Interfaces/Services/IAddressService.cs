using DainnUser.Core.Models.Address;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for managing user addresses.
/// </summary>
public interface IAddressService
{
    /// <summary>
    /// Gets all addresses for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of addresses for the user.</returns>
    Task<IReadOnlyList<AddressDto>> GetAddressesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The address if found.</returns>
    Task<AddressDto> GetAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="dto">The address data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created address.</returns>
    Task<AddressDto> AddAddressAsync(Guid userId, AddAddressDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="dto">The address update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated address.</returns>
    Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets an address as the default for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated address.</returns>
    Task<AddressDto> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}
