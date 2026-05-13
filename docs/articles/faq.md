# Frequently Asked Questions — DainnUser

Common questions organized by category.

---

## General

### Q1: What is DainnUser?

DainnUser is a .NET class library providing comprehensive user management features: authentication, authorization, profile management, and security. It is distributed via NuGet and can be integrated into any .NET application.

**Key Features:**
- JWT-based authentication with refresh tokens
- Email verification and password reset flows
- Two-factor authentication (2FA/TOTP)
- Social login (Google, Facebook, GitHub, Microsoft)
- Role-based access control (RBAC)
- Profile management with avatar uploads
- Session management and activity logging
- OWASP Top 10 compliance built-in

**Documentation:** [Architecture](architecture.md) | [Security Guide](security.md)

---

### Q2: Which .NET versions are supported?

DainnUser requires .NET 8.0 or later. It supports the current LTS version of .NET.

**Verification:**
```bash
dotnet --version
# Should output 8.0 or higher
```

---

### Q3: Which databases are supported?

DainnUser supports any EF Core-compatible database through the provider model:

| Provider | Package | Connection String Key |
|---|---|---|
| SQL Server | Microsoft.EntityFrameworkCore.SqlServer | `Server=...` |
| PostgreSQL | Npgsql.EntityFrameworkCore.PostgreSQL | `Host=...` |
| MySQL | Pomelo.EntityFrameworkCore.MySql | `Server=...` |
| SQLite | Microsoft.EntityFrameworkCore.Sqlite | `Data Source=...` |

**See:** [Configuration Reference](configuration.md#database-configuration)

---

### Q4: How do I get started quickly?

1. **Install via NuGet:**
```bash
dotnet add package DainnUser
```

2. **Configure appsettings.json:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SQLite",
      "ConnectionString": "Data Source=dainnuser.db"
    },
    "Jwt": {
      "SecretKey": "your-secret-key-min-32-characters-long",
      "Issuer": "https://yourdomain.com",
      "Audience": "https://yourdomain.com"
    }
  }
}
```

3. **Register services:**
```csharp
builder.Services.AddDainnUser(builder.Configuration);
```

4. **Use middleware:**
```csharp
app.UseDainnUser();
```

5. **Run migrations:**
```bash
dotnet ef database update
```

**Full Guide:** [Getting Started](getting-started.md)

---

## Installation

### Q5: Why does `dotnet add package DainnUser` fail?

Common causes and solutions:

1. **NuGet source not configured:**
```bash
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org
```

2. **Package name typo:** Use exact name `DainnUser` (case-sensitive).

3. **Version not available:** Check available versions:
```bash
dotnet package search DainnUser --take 10
```

4. **Network connectivity:** Check your internet connection and proxy settings.

**See:** [Troubleshooting - Issue 1](troubleshooting.md#issue-1-nuget-package-installation-failure)

---

### Q6: Do I need all DainnUser packages?

No. DainnUser is structured as a meta-package that references all components. You can install individual packages if you only need specific functionality:

| Package | When to Use |
|---|---|
| `DainnUser` | Complete package (all features) |
| `DainnUser.Core` | Domain models and interfaces only |
| `DainnUser.Application` | Business logic services |
| `DainnUser.Api` | REST API controllers |
| `DainnUser.Web` | Razor/Blazor components |

**Recommendation:** Use the meta-package `DainnUser` for simplicity.

---

## Configuration

### Q7: What configuration is required?

**Minimum required settings:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "..."
    },
    "Jwt": {
      "SecretKey": "min-32-characters",
      "Issuer": "...",
      "Audience": "..."
    }
  }
}
```

All other settings have sensible defaults.

**See:** [Configuration Reference](configuration.md)

---

### Q8: How do I use environment variables for configuration?

Use double underscore (`__`) as the path separator:

```bash
# Example environment variables
DainnUser__Database__Provider=PostgreSQL
DainnUser__Database__ConnectionString="Host=localhost;Database=myapp"
DainnUser__Jwt__SecretKey="your-secret-key"
DainnUser__Jwt__Issuer="https://yourdomain.com"
```

**Note:** Environment variables take precedence over `appsettings.json`.

---

### Q9: Can I customize DainnUser behavior in code?

Yes. Pass options to `AddDainnUser()`:

```csharp
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    options.EnableSocialLogin = true;
    options.EnableTwoFactor = true;
    options.RequireEmailVerification = true;
    options.EnableAccountLockout = true;
    options.EnableSessionManagement = true;
    options.EnableActivityLogging = true;
});
```

**See:** [Features Configuration](configuration.md#feature-flags)

---

## Features

### Q10: How does email verification work?

1. User registers with email
2. System generates a verification token (valid 24 hours)
3. Email is sent with verification link containing token
4. User clicks link → token validated → account activated
5. User can now login

**Verification Flow:**
```csharp
// Registration generates token
var userId = await _authService.RegisterAsync(email, username, password);

// Email verification validates token
await _authService.VerifyEmailAsync(userId, token);

// Resend verification if needed (revokes old token, generates new)
await _authService.ResendVerificationEmailAsync(email);
```

**See:** [API Endpoints - Verify Email](api-endpoints.md#2-verify-email)

---

### Q11: How do I enable two-factor authentication (2FA)?

**Step 1: Setup 2FA**
```bash
curl -X POST https://localhost:5001/api/auth/2fa/setup \
  -H "Authorization: Bearer YOUR_TOKEN"
```

Returns QR code URI and secret. User scans with authenticator app (Google Authenticator, Authy, etc.).

**Step 2: Enable 2FA**
```bash
curl -X POST https://localhost:5001/api/auth/2fa/enable \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"code": "123456"}'
```

Returns 10 backup codes. User must save these securely.

**See:** [API Endpoints - 2FA](api-endpoints.md#10-setup-two-factor-authentication)

---

### Q12: How does social login work?

1. User clicks social login button (Google, Facebook, etc.)
2. Redirect to OAuth provider authorization page
3. User grants permission
4. OAuth provider redirects back with authorization code
5. DainnUser exchanges code for access token
6. User info retrieved and user account created/linked
7. JWT token issued to user

**Configuration:**
```json
{
  "DainnUser": {
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "...",
        "ClientSecret": "..."
      }
    }
  }
}
```

**See:** [OAuth Configuration](configuration.md#oauth-configuration)

---

### Q13: How do I customize user entity?

Extend the base `User` class:

```csharp
// Your custom user entity
public class MyAppUser : DainnUser.Core.Entities.User
{
    public string CompanyName { get; set; }
    public string Department { get; set; }
    public DateTime? HireDate { get; set; }
}

// Register custom user
builder.Services.AddDainnUser<MyAppUser>(builder.Configuration);
```

**See:** [Getting Started - Extend User Entity](getting-started.md#extend-user-entity)

---

## Security

### Q14: How are passwords stored?

DainnUser uses ASP.NET Core Identity's password hasher (default: PBKDF2, configurable to Argon2).

**Password hashing uses:**
- Per-user unique salt
- Configurable work factor (iterations)
- Secure hash algorithm (PBKDF2-SHA256 or Argon2)

**Configuration:**
```json
{
  "DainnUser": {
    "Security": {
      "PasswordHasher": "Argon2",
      "PasswordRequirements": {
        "RequiredLength": 12,
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireNonAlphanumeric": true
      }
    }
  }
}
```

**See:** [Security Guide](security.md)

---

### Q15: How does account lockout work?

After `MaxFailedAccessAttempts` failed login attempts, the account is locked for `LockoutDurationMinutes`.

**Default configuration:**
```json
{
  "DainnUser": {
    "Security": {
      "Lockout": {
        "Enabled": true,
        "MaxFailedAccessAttempts": 5,
        "LockoutDurationMinutes": 15
      }
    }
  }
}
```

**Unlock via admin:**
```bash
curl -X POST https://localhost:5001/api/auth/admin/unlock-account/{userId} \
  -H "Authorization: Bearer ADMIN_TOKEN"
```

**See:** [Troubleshooting - Issue 9](troubleshooting.md#issue-9-account-locked-out)

---

### Q16: Is DainnUser OWASP compliant?

Yes. DainnUser is designed with OWASP Top 10 2021 compliance built-in:

| OWASP Category | Protection |
|---|---|
| A01: Broken Access Control | RBAC, policy-based authorization |
| A02: Cryptographic Failures | PBKDF2/Argon2, secure token generation |
| A03: Injection | Parameterized queries (EF Core), FluentValidation |
| A04: Insecure Design | Secure defaults, rate limiting, lockout |
| A05: Security Misconfiguration | Secure defaults, validation on startup |
| A06: Vulnerable Components | Regular dependency audits |
| A07: Identification Failures | Strong passwords, 2FA, session management |
| A08: Data Integrity Failures | Signed packages, audit logging |
| A09: Logging Failures | Login history, activity tracking |
| A10: SSRF | URL validation, input sanitization |

**See:** [Security Guide](security.md) | [OWASP Top 10](security.md#owasp-top-10-compliance)

---

## Deployment

### Q17: How do I deploy to production?

**Prerequisites:**
1. HTTPS enabled (required for OAuth)
2. Production database configured
3. Environment variables set for secrets
4. Health check endpoint added

**Production checklist:**
```bash
# Environment variables (never commit secrets!)
export DainnUser__Jwt__SecretKey="production-secret-key-here"
export DainnUser__Database__ConnectionString="Server=prod-db;Database=myapp;User=app;Password=***"

# Run in production mode
dotnet run --configuration Production

# Or use container
docker run -e DainnUser__Jwt__SecretKey="..." myapp
```

**Health check:**
```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
```

**See:** [Security Guide - Checklist](security.md#security-checklist)

---

### Q18: Can I use Docker with DainnUser?

Yes. Create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

Build and run:
```bash
docker build -t myapp .
docker run -d -p 8080:8080 \
  -e DainnUser__Jwt__SecretKey="your-secret-key" \
  -e DainnUser__Database__ConnectionString="Host=db;Database=myapp" \
  myapp
```

---

## Customization

### Q19: How do I add custom validation rules?

Create a custom validator:

```csharp
public class CustomRegisterValidator : AbstractValidator<RegisterDto>
{
    public CustomRegisterValidator()
    {
        RuleFor(x => x.Email)
            .Must(BeCompanyEmail)
            .WithMessage("Only company emails are allowed");

        RuleFor(x => x.Username)
            .Matches(@"^[a-zA-Z][a-zA-Z0-9]*$")
            .WithMessage("Username must start with a letter and contain only alphanumeric characters");
    }

    private bool BeCompanyEmail(string email)
    {
        return email.EndsWith("@mycompany.com");
    }
}

// Register custom validator
builder.Services.AddScoped<IValidator<RegisterDto>, CustomRegisterValidator>();
```

**See:** [Getting Started - Custom Validation](getting-started.md#custom-validation-rules)

---

### Q20: How do I override default services?

Replace DainnUser services with your own implementation:

```csharp
// Override email service
builder.Services.AddScoped<IEmailService, MyCustomEmailService>();

// Custom email service implementation
public class MyCustomEmailService : IEmailService
{
    public async Task SendAsync(EmailMessage message)
    {
        // Your custom implementation
        await _emailClient.SendAsync(message);
    }
}
```

**Overrideable services:**
- `IEmailService` - Email sending
- `ISmsService` - SMS sending
- `IStorageService` - File storage
- `IValidator<T>` - Validation rules

---

## Performance

### Q21: How do I improve query performance?

1. **Use pagination for lists:**
```csharp
var users = await _userService.GetUsersAsync(page: 1, pageSize: 20);
```

2. **Use AsNoTracking for read-only queries:**
```csharp
var user = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Email == email);
```

3. **Add database indexes:**
```sql
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Status ON Users(Status);
```

4. **Enable response compression:**
```csharp
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});
```

**See:** [Troubleshooting - Issue 14](troubleshooting.md#issue-14-slow-user-list-queries)

---

### Q22: How do I handle rate limiting?

DainnUser includes built-in rate limiting:

```json
{
  "DainnUser": {
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "MaxRequestsPerMinute": 60,
        "Rules": [
          {
            "Endpoint": "/api/auth/login",
            "MaxRequests": 5,
            "WindowSeconds": 60
          },
          {
            "Endpoint": "/api/auth/register",
            "MaxRequests": 3,
            "WindowSeconds": 300
          }
        ]
      }
    }
  }
}
```

**See:** [Configuration Reference - Rate Limiting](configuration.md#security-configuration)

---

## See Also

- [Getting Started](getting-started.md)
- [Configuration Reference](configuration.md)
- [API Endpoints](api-endpoints.md)
- [Troubleshooting](troubleshooting.md)
- [Code Examples](code-examples.md)