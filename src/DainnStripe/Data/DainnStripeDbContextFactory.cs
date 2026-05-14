using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DainnStripe.Data;

/// <summary>
/// Design-time factory for DainnStripe migrations.
/// </summary>
public class DainnStripeDbContextFactory : IDesignTimeDbContextFactory<DainnStripeDbContext>
{
    /// <inheritdoc />
    public DainnStripeDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DainnStripeDbContext>();
        optionsBuilder.UseSqlite("Data Source=dainnstripe.db");
        return new DainnStripeDbContext(optionsBuilder.Options);
    }
}
