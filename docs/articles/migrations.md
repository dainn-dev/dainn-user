# Database Migrations Guide

## Overview

DainnUser sử dụng Entity Framework Core Migrations để quản lý database schema. Library hỗ trợ 4 database providers: SQL Server, PostgreSQL, MySQL, và SQLite.

## Important Notes

⚠️ **Migrations đã được pre-generated** trong library với SQLite làm design-time provider. Tuy nhiên, bạn có thể generate lại migrations cho provider cụ thể của mình nếu cần.

## Configuration

Trong `appsettings.json` của ứng dụng:

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",  // SqlServer | PostgreSQL | MySQL | SQLite
      "ConnectionString": "Server=localhost;Database=DainnUserDb;Trusted_Connection=True;TrustServerCertificate=True;",
      "EnableSensitiveDataLogging": false,  // Only true in development
      "EnableDetailedErrors": false  // Only true in development
    }
  }
}
```

## Applying Migrations

### Option 1: Automatic Migration on Startup (Recommended for Development)

Trong `Program.cs`:

```csharp
using DainnUser.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Add DainnUser DbContext
builder.Services.AddDainnUserDbContext(builder.Configuration);

var app = builder.Build();

// Apply migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DainnUserDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
```

### Option 2: Manual Migration via CLI

```bash
# Navigate to your application project (not DainnUser.Infrastructure)
cd YourApp

# Apply migrations
dotnet ef database update --project ../path/to/DainnUser.Infrastructure

# Or if you have the connection string
dotnet ef database update --project ../path/to/DainnUser.Infrastructure --connection "YourConnectionString"
```

### Option 3: SQL Script Generation (Recommended for Production)

```bash
# Generate SQL script for review before applying
dotnet ef migrations script --project ../path/to/DainnUser.Infrastructure --output migration.sql

# Review migration.sql, then apply manually to production database
```

## Creating New Migrations (For Library Developers)

Nếu bạn đang phát triển DainnUser library và cần tạo migration mới:

```bash
# Navigate to Infrastructure project
cd src/DainnUser.Infrastructure

# Create new migration
dotnet ef migrations add YourMigrationName --output-dir Data/Migrations

# Remove last migration if needed
dotnet ef migrations remove
```

## Multi-Provider Considerations

### Design-Time Provider

Library sử dụng SQLite làm design-time provider (trong `DainnUserDbContextFactory.cs`) để generate migrations. Migrations này **tương thích với tất cả providers** vì:

- Sử dụng EF Core abstractions
- Không có provider-specific syntax
- Fluent API configurations hoạt động trên mọi provider

### Runtime Provider

Provider thực tế được chọn tại runtime dựa trên configuration:

```csharp
services.AddDainnUserDbContext(configuration);
```

### Provider-Specific Notes

**SQL Server:**
- Hỗ trợ đầy đủ tất cả features
- Connection retry với exponential backoff
- Recommended cho production

**PostgreSQL:**
- Case-sensitive table/column names
- Excellent performance
- Recommended cho production

**MySQL:**
- Auto-detect server version
- Sử dụng Pomelo provider (recommended)
- Recommended cho production

**SQLite:**
- File-based database
- Không hỗ trợ một số advanced features (e.g., multiple concurrent writes)
- Recommended cho development/testing only

## Seed Data

Initial migration bao gồm seed data cho 3 default roles:

- **Administrator**: Full system access
- **User**: Standard user permissions
- **Moderator**: User management permissions

Seed data được apply tự động khi chạy migration.

## Troubleshooting

### "Unable to create DbContext" Error

Đảm bảo bạn đã:
1. Add DainnUser DbContext vào DI container: `services.AddDainnUserDbContext(configuration)`
2. Configure connection string trong appsettings.json
3. Install EF Core tools: `dotnet tool install --global dotnet-ef`

### Migration Already Applied

```bash
# Check migration status
dotnet ef migrations list --project ../path/to/DainnUser.Infrastructure

# Rollback to specific migration
dotnet ef database update PreviousMigrationName --project ../path/to/DainnUser.Infrastructure
```

### Provider-Specific Connection String Issues

**SQL Server:**
```
Server=localhost;Database=DainnUserDb;Trusted_Connection=True;TrustServerCertificate=True;
```

**PostgreSQL:**
```
Host=localhost;Database=dainnuserdb;Username=postgres;Password=yourpassword
```

**MySQL:**
```
Server=localhost;Database=dainnuserdb;User=root;Password=yourpassword
```

**SQLite:**
```
Data Source=dainnuser.db
```

## Production Deployment

### Recommended Approach

1. **Generate SQL script** từ migrations
2. **Review script** với DBA team
3. **Test script** trên staging environment
4. **Apply script** manually trên production database
5. **Verify** bằng cách check `__EFMigrationsHistory` table

### Avoid Automatic Migration in Production

❌ **Không nên:**
```csharp
dbContext.Database.Migrate(); // Dangerous in production
```

✅ **Nên:**
```bash
dotnet ef migrations script --idempotent --output production-migration.sql
# Review và apply manually
```

## References

- [EF Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [EF Core Database Providers](https://learn.microsoft.com/en-us/ef/core/providers/)
