using DainnUser.Core.Entities;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Address;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for address management.
/// </summary>
public class AddressService : IAddressService
{
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="addressRepository">The address repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public AddressService(
        IUserRepository userRepository,
        IAddressRepository addressRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AddressDto>> GetAddressesAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var addresses = await _addressRepository.GetByUserIdAsync(userId, ct);
        return addresses.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<AddressDto> GetAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);
        return MapToDto(address);
    }

    /// <inheritdoc/>
    public async Task<AddressDto> AddAddressAsync(Guid userId, AddAddressDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await EnsureUserExistsAsync(userId, ct);

        var now = DateTime.UtcNow;
        var address = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AddressType = dto.AddressType ?? string.Empty,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            StateProvince = dto.StateProvince,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (dto.SetAsDefault)
        {
            await _addressRepository.ClearDefaultForUserAsync(userId, ct);
            address.IsDefault = true;
        }
        else
        {
            var hasAddresses = await _addressRepository.UserHasAddressesAsync(userId, ct);
            address.IsDefault = !hasAddresses;
        }

        await _addressRepository.AddAsync(address, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(address);
    }

    /// <inheritdoc/>
    public async Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);

        if (dto.AddressType is not null)
            address.AddressType = dto.AddressType;
        if (dto.AddressLine1 is not null)
            address.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 is not null)
            address.AddressLine2 = dto.AddressLine2;
        if (dto.City is not null)
            address.City = dto.City;
        if (dto.StateProvince is not null)
            address.StateProvince = dto.StateProvince;
        if (dto.PostalCode is not null)
            address.PostalCode = dto.PostalCode;
        if (dto.Country is not null)
            address.Country = dto.Country;

        address.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToDto(address);
    }

    /// <inheritdoc/>
    public async Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);

        if (address.IsDefault)
        {
            var remaining = await _addressRepository.GetByUserIdAsync(userId, ct);
            var first = remaining.FirstOrDefault(a => a.Id != addressId);
            if (first is not null)
            {
                first.IsDefault = true;
                first.UpdatedAt = DateTime.UtcNow;
            }
        }

        _addressRepository.Remove(address);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<AddressDto> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);

        await _addressRepository.ClearDefaultForUserAsync(userId, ct);
        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToDto(address);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken ct)
    {
        var exists = await _userRepository.AnyAsync(u => u.Id == userId, ct);
        if (!exists)
        {
            throw new UserNotFoundException(userId);
        }
    }

    private async Task<UserAddress> GetAddressForUserAsync(Guid userId, Guid addressId, CancellationToken ct)
    {
        var address = await _addressRepository.GetByUserIdAndIdAsync(userId, addressId, ct);
        if (address is null)
        {
            throw new AddressNotFoundException(addressId);
        }
        return address;
    }

    private static AddressDto MapToDto(UserAddress address)
    {
        return new AddressDto
        {
            Id = address.Id,
            AddressType = address.AddressType,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            StateProvince = address.StateProvince,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }
}
