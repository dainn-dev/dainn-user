using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DainnUser.OpenIddict.Data;

/// <summary>
/// Design-time factory for generating OpenIddict store migrations.
/// </summary>
public class DainnUserOpenIddictDbContextFactory : IDesignTimeDbContextFactory<DainnUserOpenIddictDbContext>
{
    /// <inheritdoc />
    public DainnUserOpenIddictDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DainnUserOpenIddictDbContext>();
        optionsBuilder.UseSqlite("Data Source=dainnuser-openiddict.db");
        return new DainnUserOpenIddictDbContext(optionsBuilder.Options);
    }
}
