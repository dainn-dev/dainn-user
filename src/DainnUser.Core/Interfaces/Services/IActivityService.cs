using DainnUser.Core.Enums;
using DainnUser.Core.Models.Activity;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for logging and retrieving user activity.
/// </summary>
public interface IActivityService
{
    /// <summary>
    /// Logs a user activity.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="activityType">The type of activity.</param>
    /// <param name="description">Optional description of the activity.</param>
    /// <param name="ipAddress">The IP address from which the activity was performed.</param>
    /// <param name="userAgent">The user agent string.</param>
    /// <param name="metadata">Optional metadata as JSON string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task LogActivityAsync(
        Guid userId,
        ActivityType activityType,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets activity logs for a user with pagination.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="activityType">Optional filter by activity type.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the activity logs and total count.</returns>
    Task<(IEnumerable<ActivityLogDto> Items, int TotalCount)> GetActivityLogAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        ActivityType? activityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent activity logs for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="count">The number of recent entries to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of recent activity logs.</returns>
    Task<IEnumerable<ActivityLogDto>> GetRecentActivityAsync(
        Guid userId,
        int count = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old activity logs before a specific date.
    /// </summary>
    /// <param name="beforeDate">The cutoff date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of logs removed.</returns>
    Task<int> CleanupOldLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
}
