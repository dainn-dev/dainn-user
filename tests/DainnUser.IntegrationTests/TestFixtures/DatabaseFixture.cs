using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.IntegrationTests.TestFixtures;

/// <summary>
/// Test fixture for setting up in-memory database for integration tests.
/// </summary>
public class DatabaseFixture : IDisposable
{
    public DainnUserDbContext DbContext { get; private set; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<DainnUserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        DbContext = new DainnUserDbContext(options);
        DbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        DbContext.Database.EnsureDeleted();
        DbContext.Dispose();
    }

    /// <summary>
    /// Clears all data from the database.
    /// </summary>
    public void ClearDatabase()
    {
        // Clear change tracker to avoid concurrency issues
        DbContext.ChangeTracker.Clear();

        // Remove all entities
        DbContext.ActivityLogs.RemoveRange(DbContext.ActivityLogs.ToList());
        DbContext.LoginHistories.RemoveRange(DbContext.LoginHistories.ToList());
        DbContext.UserSessions.RemoveRange(DbContext.UserSessions.ToList());
        DbContext.UserTokens.RemoveRange(DbContext.UserTokens.ToList());
        DbContext.UserClaims.RemoveRange(DbContext.UserClaims.ToList());
        DbContext.UserRoles.RemoveRange(DbContext.UserRoles.ToList());
        DbContext.UserLogins.RemoveRange(DbContext.UserLogins.ToList());
        DbContext.UserAddresses.RemoveRange(DbContext.UserAddresses.ToList());
        DbContext.UserContacts.RemoveRange(DbContext.UserContacts.ToList());
        DbContext.UserProfiles.RemoveRange(DbContext.UserProfiles.ToList());
        DbContext.Users.RemoveRange(DbContext.Users.ToList());
        DbContext.Roles.RemoveRange(DbContext.Roles.ToList());

        DbContext.SaveChanges();
        DbContext.ChangeTracker.Clear();
    }
}
