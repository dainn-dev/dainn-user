# DainnUser

.NET/C# Class Library cung cấp đầy đủ tính năng user management: authentication, authorization, profile management, và security features.

## Features

### Authentication
- User Registration với email verification
- Login với JWT tokens
- Password Reset flow
- Two-Factor Authentication (2FA)
- Social Login: Google, Facebook, GitHub, Microsoft
- Refresh Token mechanism
- Account Lockout protection

### Authorization
- Role-Based Access Control (RBAC)
- Policy-based authorization
- Permission management

### Profile Management
- User profile CRUD
- Avatar upload
- Language & timezone preferences
- Activity history
- Address & contact management

### Security
- OWASP Top 10 compliance
- Rate limiting
- Google reCAPTCHA integration
- Session management
- Login history & audit trail
- Input validation
- SQL injection prevention

## Project Structure

```
DainnUser/
├── src/
│   ├── DainnUser.Core/              # Domain entities, interfaces
│   ├── DainnUser.Infrastructure/    # EF Core, repositories
│   ├── DainnUser.Application/       # Business logic, services
│   ├── DainnUser.Api/              # API controllers
│   └── DainnUser.Web/              # Web components (Razor/Blazor)
├── tests/
│   ├── DainnUser.UnitTests/
│   ├── DainnUser.IntegrationTests/
│   └── DainnUser.SecurityTests/
└── docs/
    ├── architecture.md
    ├── getting-started.md
    └── security.md
```

## Architecture

Clean Architecture / Onion Architecture:
- **Core Layer**: Domain entities, interfaces (no dependencies)
- **Infrastructure Layer**: EF Core, database implementations
- **Application Layer**: Business logic, services
- **Presentation Layer**: API controllers, web components

## Database Support

Dynamic database provider support:
- SQL Server
- PostgreSQL
- MySQL
- SQLite

## Getting Started

See [docs/getting-started.md](docs/getting-started.md) for detailed setup instructions.

## Documentation

- [Architecture](docs/architecture.md)
- [Security Guide](docs/security.md)
- [Getting Started](docs/getting-started.md)
- [Database Migrations](docs/migrations.md)

## Development

### Build

```bash
dotnet build
```

### Run Tests

```bash
dotnet test
```

### Create NuGet Packages

```bash
dotnet pack -c Release -o nupkgs/
```

## License

MIT

## Contributing

See CONTRIBUTING.md (coming soon)
