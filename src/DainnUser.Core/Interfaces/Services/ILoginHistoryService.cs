using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service interface for login history queries and audit.
/// </summary>
public interface ILoginHistoryService
{
    /// <summary>
    /// Gets paginated login history for a user with optional date range filtering.
    /// </summary>
    Task<(IReadOnlyCollection<LoginHistory> Items, int TotalCount)> GetLoginHistoryAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent login attempts for a user.
    /// </summary>
    Task<IReadOnlyCollection<LoginHistory>> GetRecentLoginsAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts for a user since a given date.
    /// </summary>
    Task<IReadOnlyCollection<LoginHistory>> GetFailedAttemptsAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes login history records older than the configured retention period.
    /// Returns the number of records removed.
    /// </summary>
    Task<int> CleanupOldRecordsAsync(
        CancellationToken cancellationToken = default);
}
