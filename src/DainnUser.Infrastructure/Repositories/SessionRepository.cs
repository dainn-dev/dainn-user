using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserSession entity.
/// </summary>
public class SessionRepository : Repository<UserSession>, ISessionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SessionRepository(DainnUserDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<UserSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId && s.IsActive && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.IsActive && s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeactivateAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _dbSet
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var session in sessions)
        {
            session.IsActive = false;
        }
    }

    /// <inheritdoc/>
    public async Task DeactivateAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _dbSet.FindAsync(new object[] { sessionId }, cancellationToken);
        if (session != null)
        {
            session.IsActive = false;
        }
    }

    /// <inheritdoc/>
    public async Task<int> RemoveExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var expiredSessions = await _dbSet
            .Where(s => s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(expiredSessions);

        return expiredSessions.Count;
    }
}
