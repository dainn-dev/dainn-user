using DainnUser.Core.Entities;
using DainnUser.Core.Enums;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for LoginHistory entity with specific query methods.
/// </summary>
public interface ILoginHistoryRepository : IRepository<LoginHistory>
{
    /// <summary>
    /// Gets login history for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the login history entries and total count.</returns>
    Task<(IEnumerable<LoginHistory> Items, int TotalCount)> GetByUserIdAsync(Guid userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent login history for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="count">The number of recent entries to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of recent login history entries.</returns>
    Task<IEnumerable<LoginHistory>> GetRecentByUserIdAsync(Guid userId, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts for a specific user within a time window.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="since">The start time.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of failed login attempts.</returns>
    Task<IEnumerable<LoginHistory>> GetFailedAttemptsAsync(Guid userId, DateTime since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login history by provider.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="provider">The login provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of login history entries for the specified provider.</returns>
    Task<IEnumerable<LoginHistory>> GetByProviderAsync(Guid userId, LoginProvider provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login history by IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of login history entries from the specified IP.</returns>
    Task<IEnumerable<LoginHistory>> GetByIpAddressAsync(string ipAddress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets login history for a specific user with optional date range filtering and pagination.
    /// </summary>
    Task<(IEnumerable<LoginHistory> Items, int TotalCount)> GetByUserIdWithDateRangeAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes login history records older than the specified cutoff date.
    /// </summary>
    /// <param name="cutoffDate">Records with CreatedAt older than this date will be removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of records removed.</returns>
    Task<int> RemoveOldRecordsAsync(
        DateTime cutoffDate,
        CancellationToken cancellationToken = default);
}
