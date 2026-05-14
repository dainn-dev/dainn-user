using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserContact entity.
/// </summary>
public class ContactRepository : Repository<UserContact>, IContactRepository
{
    public ContactRepository(DainnUserDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<UserContact>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.ContactType)
            .ThenByDescending(c => c.IsPrimary)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserContact?> GetByUserIdAndIdAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Id == contactId, cancellationToken);
    }

    public async Task ClearPrimaryForUserAndTypeAsync(Guid userId, string contactType, CancellationToken cancellationToken = default)
    {
        var primaryContacts = await _dbSet
            .Where(c => c.UserId == userId && c.ContactType == contactType && c.IsPrimary)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var contact in primaryContacts)
        {
            contact.IsPrimary = false;
            contact.UpdatedAt = now;
        }
    }

    public async Task<UserContact?> GetPrimaryForUserAndTypeAsync(Guid userId, string contactType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.UserId == userId && c.ContactType == contactType && c.IsPrimary, cancellationToken);
    }

    public async Task<bool> UserHasContactsOfTypeAsync(Guid userId, string contactType, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(c => c.UserId == userId && c.ContactType == contactType, cancellationToken);
    }
}
