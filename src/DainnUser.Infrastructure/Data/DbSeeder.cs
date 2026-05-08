using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Data;

/// <summary>
/// Provides seed data for the database.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Seeds the database with initial data.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    public static void SeedData(ModelBuilder modelBuilder)
    {
        SeedRoles(modelBuilder);
    }

    private static void SeedRoles(ModelBuilder modelBuilder)
    {
        var now = DateTime.UtcNow;

        var roles = new[]
        {
            new Role
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Administrator",
                Description = "Full system access with all permissions",
                Permissions = "users:read,users:write,users:delete,roles:read,roles:write,roles:delete,settings:read,settings:write",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Role
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "User",
                Description = "Standard user with basic permissions",
                Permissions = "profile:read,profile:write",
                CreatedAt = now,
                UpdatedAt = now
            },
            new Role
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "Moderator",
                Description = "Moderator with user management permissions",
                Permissions = "users:read,users:write,profile:read,profile:write",
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        modelBuilder.Entity<Role>().HasData(roles);
    }
}
