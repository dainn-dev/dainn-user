using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DainnUser.Infrastructure.Data;

/// <summary>
/// Design-time factory for creating DainnUserDbContext instances.
/// Used by EF Core tools for migrations.
/// </summary>
public class DainnUserDbContextFactory : IDesignTimeDbContextFactory<DainnUserDbContext>
{
    /// <summary>
    /// Creates a new instance of DainnUserDbContext for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>A configured DainnUserDbContext instance.</returns>
    public DainnUserDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DainnUserDbContext>();

        // Use SQLite for design-time (migrations)
        // This is just for generating migrations - actual provider is configured at runtime
        optionsBuilder.UseSqlite("Data Source=dainnuser.db");

        return new DainnUserDbContext(optionsBuilder.Options);
    }
}
