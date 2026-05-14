using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for user tokens with token-specific queries.
/// </summary>
public interface IUserTokenRepository : IRepository<UserToken>
{
    Task<IEnumerable<UserToken>> GetActiveContactVerificationTokensAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default);
    Task<int> CountRecentContactVerificationTokensAsync(Guid userId, Guid contactId, DateTime sinceUtc, CancellationToken cancellationToken = default);
}
