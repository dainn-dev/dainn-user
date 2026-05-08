using DainnUser.Core.Entities;
using DainnUser.Core.Enums;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for ActivityLog entity with specific query methods.
/// </summary>
public interface IActivityLogRepository : IRepository<ActivityLog>
{
    /// <summary>
    /// Gets activity logs for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the activity logs and total count.</returns>
    Task<(IEnumerable<ActivityLog> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent activity logs for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="count">The number of recent entries to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of recent activity logs.</returns>
    Task<IEnumerable<ActivityLog>> GetRecentByUserIdAsync(Guid userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity logs by activity type.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="activityType">The activity type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of activity logs for the specified type.</returns>
    Task<IEnumerable<ActivityLog>> GetByActivityTypeAsync(Guid userId, ActivityType activityType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity logs within a date range.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="startDate">The start date.</param>
    /// <param name="endDate">The end date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of activity logs within the date range.</returns>
    Task<IEnumerable<ActivityLog>> GetByDateRangeAsync(Guid userId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity logs by IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of activity logs from the specified IP.</returns>
    Task<IEnumerable<ActivityLog>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes old activity logs before a specific date.
    /// </summary>
    /// <param name="beforeDate">The cutoff date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of logs removed.</returns>
    Task<int> RemoveOldLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
}
