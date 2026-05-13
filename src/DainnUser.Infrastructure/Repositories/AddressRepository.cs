using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserAddress entity.
/// </summary>
public class AddressRepository : Repository<UserAddress>, IAddressRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AddressRepository(DainnUserDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserAddress?> GetByUserIdAndIdAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Id == addressId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ClearDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var defaultAddresses = await _dbSet
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var address in defaultAddresses)
        {
            address.IsDefault = false;
            address.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <inheritdoc/>
    public async Task<UserAddress?> GetDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> UserHasAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(a => a.UserId == userId, cancellationToken);
    }
}
