# DainnUser Documentation

DainnUser is a comprehensive .NET 8 class library for user management, authentication, authorization, profile management, and security features. It is distributed as NuGet packages so .NET developers can integrate flexible user management into their applications.

## Key Features

- **Authentication**: JWT-based authentication with refresh token rotation
- **Social Login**: Google, Facebook, GitHub, Microsoft OAuth integration
- **Two-Factor Authentication**: TOTP-based 2FA with QR code generation
- **User Management**: Complete CRUD operations with role-based access control
- **Session Management**: Multi-device session tracking and management
- **Email Services**: Multi-provider support (SMTP, SendGrid, AWS SES)
- **Security**: Rate limiting, reCAPTCHA, account lockout, password policies
- **Storage**: Multi-provider support (Local, Azure Blob, AWS S3)
- **Database**: Support for SQL Server, PostgreSQL, MySQL, SQLite

## Quick Start

Install the core packages:

```bash
dotnet add package DainnUser.Core
dotnet add package DainnUser.Infrastructure
dotnet add package DainnUser.Application
```

Configure DainnUser services:

```csharp
builder.Services.AddDainnUser(options =>
{
    options.ConnectionString = "your-connection-string";
    options.DatabaseProvider = DatabaseProvider.SqlServer;
    options.JwtSecret = "your-secret-key-min-32-bytes";
    options.JwtIssuer = "your-issuer";
    options.JwtAudience = "your-audience";
});

builder.Services.AddDainnUserJwtAuthentication();

app.UseDainnUserMigrations();
app.UseAuthentication();
app.UseAuthorization();
```

## Documentation

- [Getting Started](articles/getting-started.md)
- [Configuration](articles/configuration.md)
- [API Reference](api/index.md)
- [Examples](articles/code-examples.md)

## Project Links

- [GitHub Repository](https://github.com/dainn/dainn-user)
- [NuGet Packages](https://www.nuget.org/packages?q=DainnUser)
