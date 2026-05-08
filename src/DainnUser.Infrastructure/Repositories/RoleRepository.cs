using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for Role entity.
/// </summary>
public class RoleRepository : Repository<Role>, IRoleRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public RoleRepository(DainnUserDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<Role>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.UserRoles.Any(ur => ur.UserId == userId))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Role?> GetWithUsersAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(r => r.UserRoles)
                .ThenInclude(ur => ur.User)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsNameTakenAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(r => r.Name == name);

        if (excludeRoleId.HasValue)
        {
            query = query.Where(r => r.Id != excludeRoleId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
