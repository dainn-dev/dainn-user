using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserToken entity.
/// </summary>
public class UserTokenRepository : Repository<UserToken>, IUserTokenRepository
{
    public UserTokenRepository(DainnUserDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserToken>> GetActiveContactVerificationTokensAsync(
        Guid userId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(t => t.UserId == userId
                        && t.ContactId == contactId
                        && t.TokenType == TokenType.ContactVerification
                        && !t.IsUsed
                        && !t.IsRevoked
                        && t.ExpiresAt > now)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountRecentContactVerificationTokensAsync(
        Guid userId,
        Guid contactId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(
            t => t.UserId == userId
                 && t.ContactId == contactId
                 && t.TokenType == TokenType.ContactVerification
                 && t.CreatedAt >= sinceUtc,
            cancellationToken);
    }
}
