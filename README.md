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

### Prerequisites

- .NET 8.0 SDK
- SMTP server for email (Gmail, SendGrid, or local SMTP for development)

### Quick Start

1. **Clone the repository**

```bash
git clone <repository-url>
cd DainnUser
```

2. **Add DainnUser to your application**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add DainnUser services with configuration
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    options.EnableSocialLogin = false;
    options.EnableTwoFactor = false;
    options.RequireEmailVerification = true;
    options.EnableAccountLockout = true;
    options.EnableSessionManagement = true;
    options.EnableActivityLogging = true;
});

var app = builder.Build();

// Add DainnUser middleware
app.UseDainnUser();

app.Run();
```

3. **Configure Email Settings**

Edit `appsettings.Development.json`:

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SQLite",
      "ConnectionString": "Data Source=dainnuser.db"
    },
    "Email": {
      "SmtpHost": "smtp.gmail.com",
      "SmtpPort": 587,
      "SmtpUsername": "your-email@gmail.com",
      "SmtpPassword": "your-app-password",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "DainnUser",
      "EnableSsl": true
    }
  }
}
```

**For local development**, use a local SMTP server like [MailHog](https://github.com/mailhog/MailHog):

```json
{
  "DainnUser": {
    "Email": {
      "SmtpHost": "localhost",
      "SmtpPort": 1025,
      "SmtpUsername": "",
      "SmtpPassword": "",
      "FromEmail": "dev@localhost",
      "FromName": "DainnUser Dev",
      "EnableSsl": false
    }
  }
}
```

4. **Run the API**

```bash
cd src/DainnUser.Api
dotnet run
```

5. **Access Swagger UI**

Open your browser and navigate to:
- https://localhost:5001/swagger (HTTPS)
- http://localhost:5000/swagger (HTTP)

### API Endpoints

See [docs/api-endpoints.md](docs/api-endpoints.md) for detailed API documentation.

**Quick Reference:**

- `POST /api/auth/register` - Register new user
- `POST /api/auth/verify-email` - Verify email address
- `POST /api/auth/resend-verification` - Resend verification email

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
