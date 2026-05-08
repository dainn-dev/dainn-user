using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LoginHistory entity.
/// </summary>
public class LoginHistoryRepository : Repository<LoginHistory>, ILoginHistoryRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginHistoryRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public LoginHistoryRepository(DainnUserDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<LoginHistory> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(lh => lh.UserId == userId)
            .OrderByDescending(lh => lh.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LoginHistory>> GetRecentByUserIdAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lh => lh.UserId == userId)
            .OrderByDescending(lh => lh.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LoginHistory>> GetFailedAttemptsAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lh => lh.UserId == userId && !lh.IsSuccessful && lh.CreatedAt >= since)
            .OrderByDescending(lh => lh.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LoginHistory>> GetByProviderAsync(Guid userId, LoginProvider provider, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lh => lh.UserId == userId && lh.Provider == provider)
            .OrderByDescending(lh => lh.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<LoginHistory>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(lh => lh.IpAddress == ipAddress)
            .OrderByDescending(lh => lh.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
