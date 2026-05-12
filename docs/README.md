# DainnUser

A comprehensive .NET 8 class library for user management, authentication, and authorization.

## Features

- **Authentication**: JWT-based authentication with refresh token rotation
- **Social Login**: Google, Facebook, GitHub, Microsoft OAuth integration
- **Two-Factor Authentication**: TOTP-based 2FA with QR code generation
- **User Management**: Complete CRUD operations with role-based access control
- **Session Management**: Multi-device session tracking and management
- **Email Services**: Multi-provider support (SMTP, SendGrid, AWS SES)
- **Security**: Rate limiting, reCAPTCHA, account lockout, password policies
- **Storage**: Multi-provider support (Local, Azure Blob, AWS S3)
- **Database**: Support for SQL Server, PostgreSQL, MySQL, SQLite

## Installation

Install the core packages via NuGet:

```bash
dotnet add package DainnUser.Core
dotnet add package DainnUser.Infrastructure
dotnet add package DainnUser.Application
```

For web UI components:

```bash
dotnet add package DainnUser.Web
```

## Quick Start

### 1. Configure Services

```csharp
builder.Services.AddDainnUser(options =>
{
    options.ConnectionString = "your-connection-string";
    options.DatabaseProvider = DatabaseProvider.SqlServer;
    options.JwtSecret = "your-secret-key-min-32-bytes";
    options.JwtIssuer = "your-issuer";
    options.JwtAudience = "your-audience";
});
```

### 2. Configure Authentication

```csharp
builder.Services.AddDainnUserJwtAuthentication();
```

### 3. Apply Migrations

```csharp
app.UseDainnUserMigrations();
```

### 4. Use Authentication

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

## Configuration

### JWT Settings

```json
{
  "DainnUser": {
    "Jwt": {
      "Secret": "your-secret-key-minimum-32-bytes",
      "Issuer": "your-issuer",
      "Audience": "your-audience",
      "ExpirationMinutes": 60
    }
  }
}
```

### Social Login

```json
{
  "DainnUser": {
    "EnableSocialLogin": true,
    "GoogleClientId": "your-google-client-id",
    "GoogleClientSecret": "your-google-client-secret",
    "FacebookAppId": "your-facebook-app-id",
    "FacebookAppSecret": "your-facebook-app-secret",
    "GitHubClientId": "your-github-client-id",
    "GitHubClientSecret": "your-github-client-secret",
    "MicrosoftClientId": "your-microsoft-client-id",
    "MicrosoftClientSecret": "your-microsoft-client-secret"
  }
}
```

### Email Configuration

```json
{
  "DainnUser": {
    "Email": {
      "Provider": "Smtp",
      "Smtp": {
        "Host": "smtp.gmail.com",
        "Port": 587,
        "Username": "your-email@gmail.com",
        "Password": "your-password",
        "EnableSsl": true,
        "FromEmail": "noreply@yourdomain.com",
        "FromName": "Your App"
      }
    }
  }
}
```

## Usage Examples

### Register a User

```csharp
var result = await authService.RegisterAsync(new RegisterDto
{
    Email = "user@example.com",
    Username = "username",
    Password = "SecurePassword123!"
});
```

### Login

```csharp
var result = await authService.LoginAsync(new LoginDto
{
    Email = "user@example.com",
    Password = "SecurePassword123!"
});

var accessToken = result.AccessToken;
var refreshToken = result.RefreshToken;
```

### Social Login

```csharp
var result = await socialLoginService.LoginWithGoogleAsync(
    authorizationCode: "google-auth-code",
    callbackUrl: "https://yourapp.com/signin-google",
    ipAddress: "127.0.0.1",
    userAgent: "Mozilla/5.0"
);
```

### Enable Two-Factor Authentication

```csharp
var setup = await twoFactorService.SetupTwoFactorAsync(userId);
var qrCodeUrl = setup.QrCodeUrl; // Display to user
```

## Documentation

- [Getting Started](https://github.com/dainn/dainn-user/blob/master/docs/getting-started.md)
- [Architecture](https://github.com/dainn/dainn-user/blob/master/docs/architecture.md)
- [Security](https://github.com/dainn/dainn-user/blob/master/docs/security.md)
- [API Endpoints](https://github.com/dainn/dainn-user/blob/master/docs/api-endpoints.md)
- [Migrations](https://github.com/dainn/dainn-user/blob/master/docs/migrations.md)

## Requirements

- .NET 8.0 or later
- Entity Framework Core 8.0 or later
- Supported databases: SQL Server, PostgreSQL, MySQL, SQLite

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/dainn/dainn-user).
