using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation for managing transactions across multiple repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly DainnUserDbContext _context;
    private IDbContextTransaction? _transaction;

    private IUserRepository? _users;
    private IRoleRepository? _roles;
    private ILoginHistoryRepository? _loginHistories;
    private ISessionRepository? _sessions;
    private IActivityLogRepository? _activityLogs;
    private IAddressRepository? _addresses;
    private IContactRepository? _contacts;
    private IUserTokenRepository? _userTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="UnitOfWork"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UnitOfWork(DainnUserDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public IUserRepository Users => _users ??= new UserRepository(_context);

    /// <inheritdoc/>
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);

    /// <inheritdoc/>
    public ILoginHistoryRepository LoginHistories => _loginHistories ??= new LoginHistoryRepository(_context);

    /// <inheritdoc/>
    public ISessionRepository Sessions => _sessions ??= new SessionRepository(_context);

    /// <inheritdoc/>
    public IActivityLogRepository ActivityLogs => _activityLogs ??= new ActivityLogRepository(_context);

    /// <inheritdoc/>
    public IAddressRepository Addresses => _addresses ??= new AddressRepository(_context);

    /// <inheritdoc/>
    public IContactRepository Contacts => _contacts ??= new ContactRepository(_context);

    /// <inheritdoc/>
    public IUserTokenRepository UserTokens => _userTokens ??= new UserTokenRepository(_context);

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to commit.");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc/>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No active transaction to rollback.");
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
