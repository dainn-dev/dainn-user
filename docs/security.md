# Security Guide — DainnUser

Best practices và security features của DainnUser library.

## OWASP Top 10 Compliance

DainnUser được thiết kế để comply với OWASP Top 10 2021 out-of-the-box.

### A01: Broken Access Control ✓

**Built-in Protection:**
- Role-Based Access Control (RBAC)
- Policy-based authorization
- Resource ownership validation
- Automatic authorization checks

**Usage:**
```csharp
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteUser(int id)
{
    // Additional ownership check
    if (!await _authService.CanDeleteUserAsync(User, id))
        return Forbid();
    
    await _userService.DeleteAsync(id);
    return NoContent();
}
```

### A02: Cryptographic Failures ✓

**Built-in Protection:**
- PBKDF2/Argon2 password hashing
- Secure token generation
- Data Protection API integration
- TLS/HTTPS enforcement

**Configuration:**
```json
{
  "DainnUser": {
    "Security": {
      "PasswordHasher": "Argon2", // or "PBKDF2"
      "RequireHttps": true
    }
  }
}
```

### A03: Injection ✓

**Built-in Protection:**
- Parameterized queries (EF Core)
- Input validation (FluentValidation)
- Output encoding
- SQL injection prevention

**All database queries use EF Core parameterization:**
```csharp
// Safe - parameterized by EF Core
var user = await _context.Users
    .Where(u => u.Email == email)
    .FirstOrDefaultAsync();
```

### A04: Insecure Design ✓

**Built-in Protection:**
- Secure by default configuration
- Rate limiting
- Account lockout
- Session timeout
- Brute force protection

**Configuration:**
```json
{
  "DainnUser": {
    "Security": {
      "Lockout": {
        "MaxFailedAccessAttempts": 5,
        "LockoutDurationMinutes": 15
      },
      "RateLimiting": {
        "Enabled": true,
        "MaxRequestsPerMinute": 60
      }
    }
  }
}
```

### A05: Security Misconfiguration ✓

**Built-in Protection:**
- Secure defaults
- Configuration validation on startup
- Security headers
- Error message sanitization

**Security Headers:**
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    await next();
});
```

### A06: Vulnerable and Outdated Components ✓

**Best Practices:**
- Keep DainnUser updated
- Regular dependency audits
- Automated vulnerability scanning

**Check for vulnerabilities:**
```bash
dotnet list package --vulnerable --include-transitive
```

### A07: Identification and Authentication Failures ✓

**Built-in Protection:**
- Strong password requirements
- 2FA support (TOTP)
- Session management
- Login history tracking
- Credential stuffing protection

**Password Requirements:**
```json
{
  "DainnUser": {
    "Security": {
      "PasswordRequirements": {
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireNonAlphanumeric": true,
        "RequiredLength": 12,
        "RequiredUniqueChars": 4
      }
    }
  }
}
```

### A08: Software and Data Integrity Failures ✓

**Built-in Protection:**
- Signed NuGet packages
- Integrity checks
- Audit logging

### A09: Security Logging and Monitoring Failures ✓

**Built-in Protection:**
- Login attempt logging
- Failed authorization logging
- Activity tracking
- Audit trail

**Logging:**
```csharp
// Automatic logging of security events
_logger.LogWarning("Failed login attempt for user {Email} from {IpAddress}", 
    email, ipAddress);
```

### A10: Server-Side Request Forgery (SSRF) ✓

**Built-in Protection:**
- URL validation
- Whitelist external domains
- Input sanitization

## Authentication Security

### JWT Token Security

**Best Practices:**
- Short expiration time (15-60 minutes)
- Use refresh tokens
- Rotate refresh tokens
- Store tokens securely (HttpOnly cookies)

**Configuration:**
```json
{
  "DainnUser": {
    "Jwt": {
      "ExpirationMinutes": 15,
      "RefreshTokenExpirationDays": 7,
      "RotateRefreshTokens": true
    }
  }
}
```

### Two-Factor Authentication (2FA)

**Enable 2FA:**
```csharp
var result = await _twoFactorService.EnableAsync(userId);
// User scans QR code with authenticator app
```

**Verify 2FA:**
```csharp
var isValid = await _twoFactorService.VerifyAsync(userId, code);
```

### Social Login Security

**OAuth Configuration:**
```json
{
  "DainnUser": {
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "...",
        "ClientSecret": "...",
        "CallbackPath": "/signin-google"
      }
    }
  }
}
```

**Security Notes:**
- Always use HTTPS for OAuth callbacks
- Validate state parameter
- Verify token signatures
- Use PKCE for mobile apps

## Session Management

### Secure Session Configuration

```json
{
  "DainnUser": {
    "Session": {
      "TimeoutMinutes": 30,
      "SlidingExpiration": true,
      "MaxActiveSessions": 5
    }
  }
}
```

### Session Revocation

```csharp
// Revoke specific session
await _sessionService.RevokeAsync(sessionId);

// Revoke all sessions for user
await _sessionService.RevokeAllAsync(userId);
```

## Input Validation

### FluentValidation Rules

```csharp
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);
        
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase")
            .Matches(@"[a-z]").WithMessage("Must contain lowercase")
            .Matches(@"[0-9]").WithMessage("Must contain digit")
            .Matches(@"[\W]").WithMessage("Must contain special character");
        
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches(@"^[a-zA-Z0-9_-]+$")
            .WithMessage("Username can only contain letters, numbers, underscore, and dash");
    }
}
```

## Rate Limiting

### Configuration

```json
{
  "DainnUser": {
    "RateLimiting": {
      "Enabled": true,
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
```

## CSRF Protection

### Enable Anti-Forgery Tokens

```csharp
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

### Use in Controllers

```csharp
[ValidateAntiForgeryToken]
[HttpPost]
public async Task<IActionResult> UpdateProfile([FromBody] ProfileDto dto)
{
    // ...
}
```

## XSS Protection

### Output Encoding

DainnUser automatically encodes output. For custom views:

```razor
@* Automatic encoding *@
<p>@Model.UserInput</p>

@* Raw HTML (use with caution) *@
<p>@Html.Raw(Model.TrustedHtml)</p>
```

### Content Security Policy

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy", 
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'");
    await next();
});
```

## Secure Cookie Configuration

```csharp
services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.SlidingExpiration = true;
});
```

## Audit Logging

### Enable Audit Trail

```json
{
  "DainnUser": {
    "Audit": {
      "Enabled": true,
      "LogLevel": "Information",
      "IncludeIpAddress": true,
      "IncludeUserAgent": true
    }
  }
}
```

### Query Audit Logs

```csharp
var logs = await _activityService.GetActivityLogAsync(userId, 
    startDate: DateTime.UtcNow.AddDays(-30),
    endDate: DateTime.UtcNow);
```

## Security Testing

### Run Security Tests

```bash
dotnet test tests/DainnUser.SecurityTests/
```

### OWASP ZAP Scanning

```bash
# Start your app
dotnet run

# Run ZAP scan
zap-cli quick-scan --self-contained http://localhost:5000
```

## Security Checklist

Before deploying to production:

- [ ] HTTPS enforced
- [ ] Strong password requirements configured
- [ ] 2FA enabled for admin accounts
- [ ] Rate limiting configured
- [ ] Session timeout configured
- [ ] CSRF protection enabled
- [ ] Security headers configured
- [ ] Audit logging enabled
- [ ] Dependencies up to date
- [ ] Security tests passing
- [ ] Secrets not in source control
- [ ] Database connection strings encrypted
- [ ] Error messages don't leak sensitive info
- [ ] CORS properly configured
- [ ] File upload validation (if applicable)

## Reporting Security Issues

If you discover a security vulnerability in DainnUser:

1. **DO NOT** open a public GitHub issue
2. Email security@dainnuser.com with details
3. Include steps to reproduce
4. We'll respond within 48 hours

## Security Updates

Subscribe to security advisories:
- GitHub Security Advisories
- NuGet package updates
- Security mailing list: security-announce@dainnuser.com
