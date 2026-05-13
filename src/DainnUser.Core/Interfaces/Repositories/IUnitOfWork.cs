namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Unit of Work pattern interface for managing transactions across multiple repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Gets the user repository.
    /// </summary>
    IUserRepository Users { get; }

    /// <summary>
    /// Gets the role repository.
    /// </summary>
    IRoleRepository Roles { get; }

    /// <summary>
    /// Gets the login history repository.
    /// </summary>
    ILoginHistoryRepository LoginHistories { get; }

    /// <summary>
    /// Gets the session repository.
    /// </summary>
    ISessionRepository Sessions { get; }

    /// <summary>
    /// Gets the activity log repository.
    /// </summary>
    IActivityLogRepository ActivityLogs { get; }

    /// <summary>
    /// Gets the address repository.
    /// </summary>
    IAddressRepository Addresses { get; }

    /// <summary>
    /// Saves all changes made in this unit of work to the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
