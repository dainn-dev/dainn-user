using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for User entity.
/// </summary>
public class UserRepository : Repository<User>, IUserRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UserRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserRepository(DainnUserDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailWithTokensAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdWithTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetWithProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(u => u.Status == status)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByIdWithLoginsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Logins)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<User?> GetByExternalLoginAsync(LoginProvider provider, string providerKey, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(u => u.Logins)
            .FirstOrDefaultAsync(u => u.Logins.Any(l => l.Provider == provider && l.ProviderKey == providerKey), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddTokenAsync(UserToken token, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserToken>().AddAsync(token, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        return await _context.Set<UserToken>()
            .FirstOrDefaultAsync(
                t => t.TokenType == TokenType.RefreshToken && t.TokenValue == tokenHash,
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserToken?> GetPasswordResetTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        // Eager-load User navigation — the password-reset flow needs both the token (to mark used)
        // and the user (to update password + send confirmation email).
        return await _context.Set<UserToken>()
            .Include(t => t.User)
            .FirstOrDefaultAsync(
                t => t.TokenType == TokenType.PasswordReset && t.TokenValue == tokenHash,
                cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.Set<UserToken>()
            .Where(t => t.UserId == userId
                        && t.TokenType == TokenType.RefreshToken
                        && !t.IsRevoked
                        && !t.IsUsed)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }
    }

    /// <inheritdoc/>
    public async Task RevokeAllRefreshTokensExceptSessionAsync(Guid userId, Guid keepSessionId, CancellationToken cancellationToken = default)
    {
        // Load the session token hash to identify the refresh token to keep.
        var keepSession = await _context.Set<UserSession>()
            .Where(s => s.Id == keepSessionId && s.UserId == userId && s.IsActive)
            .Select(s => s.SessionToken)
            .FirstOrDefaultAsync(cancellationToken);

        var query = _context.Set<UserToken>()
            .Where(t => t.UserId == userId
                        && t.TokenType == TokenType.RefreshToken
                        && !t.IsRevoked
                        && !t.IsUsed);

        if (keepSession is not null)
        {
            query = query.Where(t => t.TokenValue != keepSession);
        }

        var tokens = await query.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.Email == email);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsUsernameTakenAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(u => u.Username == username);

        if (excludeUserId.HasValue)
        {
            query = query.Where(u => u.Id != excludeUserId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserProfile>().AddAsync(profile, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddLoginAsync(UserLogin login, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserLogin>().AddAsync(login, cancellationToken);
    }
}
