using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for ActivityLog entity.
/// </summary>
public class ActivityLogRepository : Repository<ActivityLog>, IActivityLogRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityLogRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public ActivityLogRepository(DainnUserDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<ActivityLog> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ActivityLog>> GetRecentByUserIdAsync(Guid userId, int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ActivityLog>> GetByActivityTypeAsync(Guid userId, ActivityType activityType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(al => al.UserId == userId && al.ActivityType == activityType)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ActivityLog>> GetByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(al => al.UserId == userId && al.CreatedAt >= startDate && al.CreatedAt <= endDate)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ActivityLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(al => al.IpAddress == ipAddress)
            .OrderByDescending(al => al.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> RemoveOldLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        var oldLogs = await _dbSet
            .Where(al => al.CreatedAt < beforeDate)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(oldLogs);

        return oldLogs.Count;
    }
}
