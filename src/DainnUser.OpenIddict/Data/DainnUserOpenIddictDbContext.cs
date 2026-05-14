using Microsoft.EntityFrameworkCore;

namespace DainnUser.OpenIddict.Data;

/// <summary>
/// EF Core context for OpenIddict stores. Kept separate from <c>DainnUserDbContext</c>
/// so the base infrastructure package does not require OpenIddict.
/// </summary>
public class DainnUserOpenIddictDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DainnUserOpenIddictDbContext"/> class.
    /// </summary>
    public DainnUserOpenIddictDbContext(DbContextOptions<DainnUserOpenIddictDbContext> options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.UseOpenIddict();
    }
}
