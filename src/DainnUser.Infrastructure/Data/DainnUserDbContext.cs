using DainnUser.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Data;

/// <summary>
/// Database context for DainnUser library.
/// </summary>
public class DainnUserDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DainnUserDbContext"/> class.
    /// </summary>
    /// <param name="options">The options to be used by the DbContext.</param>
    public DainnUserDbContext(DbContextOptions<DainnUserDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Roles DbSet.
    /// </summary>
    public DbSet<Role> Roles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserRoles DbSet.
    /// </summary>
    public DbSet<UserRole> UserRoles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserClaims DbSet.
    /// </summary>
    public DbSet<UserClaim> UserClaims { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserLogins DbSet.
    /// </summary>
    public DbSet<UserLogin> UserLogins { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserTokens DbSet.
    /// </summary>
    public DbSet<UserToken> UserTokens { get; set; } = null!;

    /// <summary>
    /// Gets or sets the LoginHistories DbSet.
    /// </summary>
    public DbSet<LoginHistory> LoginHistories { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserSessions DbSet.
    /// </summary>
    public DbSet<UserSession> UserSessions { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserProfiles DbSet.
    /// </summary>
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserAddresses DbSet.
    /// </summary>
    public DbSet<UserAddress> UserAddresses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserContacts DbSet.
    /// </summary>
    public DbSet<UserContact> UserContacts { get; set; } = null!;

    /// <summary>
    /// Gets or sets the ActivityLogs DbSet.
    /// </summary>
    public DbSet<ActivityLog> ActivityLogs { get; set; } = null!;

    /// <summary>
    /// Configures the model using Fluent API.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from the assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DainnUserDbContext).Assembly);

        // Seed initial data
        DbSeeder.SeedData(modelBuilder);
    }
}
