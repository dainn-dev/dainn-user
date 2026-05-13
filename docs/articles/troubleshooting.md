# Troubleshooting Guide — DainnUser

Common issues organized by category with symptom, root cause, solution, and prevention.

---

## Install/Setup

### Issue 1: NuGet Package Installation Failure

**Symptom:** `dotnet add package DainnUser` fails with `error: Unable to find package`

**Root Cause:**
- NuGet source not configured
- Package version does not exist
- Network connectivity issue

**Solution:**
```bash
# Check NuGet sources
dotnet nuget list source

# Add NuGet.org if missing
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

# Search for available versions
dotnet package search DainnUser --take 10

# Install specific version
dotnet add package DainnUser --version 1.0.0
```

**Prevention:** Configure `NuGet.config` with official NuGet source.

---

### Issue 2: Startup Configuration Validation Error

**Symptom:** `InvalidOperationException: DainnUser configuration validation failed.` at startup.

**Root Cause:** Missing or invalid required configuration.

**Solution:**
1. Check all required fields in `appsettings.json`:
   - `Database.ConnectionString`
   - `Jwt.SecretKey` (min 32 characters)
   - `Jwt.Issuer`
   - `Jwt.Audience`
2. Verify password requirements are valid
3. Verify database provider is supported

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=MyApp;Trusted_Connection=True;"
    },
    "Jwt": {
      "SecretKey": "at-least-32-characters-long-secure-key-here!",
      "Issuer": "https://yourdomain.com",
      "Audience": "https://yourdomain.com"
    }
  }
}
```

**Prevention:** Use `appsettings.Development.json` with development-friendly defaults.

---

### Issue 3: Missing Service Registration

**Symptom:** `System.InvalidOperationException: Unable to resolve service for type 'DainnUser.Core.Interfaces.Services.IAuthenticationService'`

**Root Cause:** `AddDainnUser()` not called in `Program.cs`.

**Solution:**
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDainnUser(builder.Configuration);
var app = builder.Build();
app.UseDainnUser();
app.Run();
```

**Prevention:** Always call `AddDainnUser()` before `Build()` and `UseDainnUser()` after.

---

## Database / Migrations

### Issue 4: Database Migrations Not Applying

**Symptom:** App starts but tables do not exist. `SqlException: Invalid object name 'Users'`.

**Root Cause:**
- Migrations not run
- Wrong database context targeted
- Connection string wrong

**Solution:**
```bash
# Verify connection string
dotnet ef dbcontext info --project src/YourApp --context DainnUserDbContext

# Apply migrations
dotnet ef database update --project src/YourApp --context DainnUserDbContext

# Check migration status
dotnet ef migrations list --project src/YourApp --context DainnUserDbContext
```

**Prevention:** Add auto-migration in development:
```csharp
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DainnUserDbContext>();
    context.Database.Migrate();
}
```

---

### Issue 5: EF Core InMemory Provider Collection Tracking Error

**Symptom:** Integration test fails with `DbUpdateConcurrencyException: Attempted to update or delete an entity that does not exist in the store.`

**Status:** Known issue with EF Core InMemory provider. **Production code is NOT affected.**

**Root Cause:** EF Core InMemory provider has known limitations with tracking modified collections when entities are loaded with navigation properties, modified, and saved in a single operation.

**Solution (for tests):** Use SQLite in-memory instead:
```csharp
var connection = new SqliteConnection("DataSource=:memory:");
connection.Open();

var options = new DbContextOptionsBuilder<DainnUserDbContext>()
    .UseSqlite(connection)
    .Options;
```

**See:** `docs/known-issues.md` for full details.

---

### Issue 6: Database Connection Timeout

**Symptom:** `Timeout expired. The timeout period elapsed prior to completion of the operation.`

**Root Cause:**
- Database server not reachable
- Firewall blocking connection
- DNS resolution failure

**Solution:**
```bash
# Test database connectivity
ping your-db-server

# Test port connectivity
telnet your-db-server 1433  # SQL Server
telnet your-db-server 5432  # PostgreSQL
telnet your-db-server 3306  # MySQL

# Check firewall rules
```

Add connection timeout to connection string:
```
"ConnectionString": "Server=localhost;Database=MyApp;Connection Timeout=30;"
```

**Prevention:** Use connection pooling and retry policies:
```csharp
builder.Services.AddDbContext<DainnUserDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
    }));
```

---

## Auth / JWT

### Issue 7: JWT Authentication Not Working (401 Unauthorized)

**Symptom:** All authenticated endpoints return `401 Unauthorized`.

**Root Cause:**
1. `UseAuthentication()` called after `UseAuthorization()` or not called at all
2. JWT secret key mismatch
3. Token expired
4. Issuer/Audience mismatch

**Solution:**
```csharp
// Order matters!
app.UseRouting();

app.UseAuthentication();  // MUST be before UseAuthorization
app.UseAuthorization();

app.MapControllers();
```

Verify JWT configuration:
```json
{
  "DainnUser": {
    "Jwt": {
      "SecretKey": "same-key-used-everywhere-min-32-characters",
      "Issuer": "https://yourdomain.com",
      "Audience": "https://yourdomain.com",
      "ExpirationMinutes": 60
    }
  }
}
```

**Prevention:** Keep JWT configuration consistent across all services.

---

### Issue 8: "Token invalid or expired" Error

**Symptom:** API returns `"Invalid refresh token"` or token errors.

**Root Cause:**
- Token has expired
- Refresh token has been revoked
- Refresh token rotation: old refresh token reused
- Clock skew between client and server

**Solution:**
1. Check token expiration time in response
2. Use refresh token before access token expires
3. Handle refresh token rotation flow properly:

```csharp
public async Task<string> GetValidAccessToken()
{
    if (_accessTokenExpiry > DateTime.UtcNow.AddMinutes(5))
        return _accessToken;

    var refreshResponse = await _httpClient.PostAsJsonAsync(
        "api/auth/refresh-token",
        new { refreshToken = _refreshToken });

    var result = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
    _accessToken = result.Data.AccessToken;
    _refreshToken = result.Data.RefreshToken; // Store new refresh token
    return _accessToken;
}
```

**Prevention:** Implement automatic token refresh with a custom `DelegatingHandler`.

---

### Issue 9: Account Locked Out

**Symptom:** Login returns `423 Locked` with "Account is locked due to too many failed login attempts."

**Root Cause:** User exceeded `MaxFailedAccessAttempts`.

**Solution (User):** Wait for lockout duration to expire (default 15 minutes).

**Solution (Admin):**
```bash
curl -X POST https://localhost:5001/api/auth/admin/unlock-account/{userId} \
  -H "Authorization: Bearer YOUR_ADMIN_JWT_TOKEN"
```

Or programmatically:
```csharp
await _authService.UnlockAccountAsync(userId);
```

**Prevention:** Configure appropriate lockout settings:
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

---

## Email

### Issue 10: Verification Email Not Sending

**Symptom:** User registers but never receives verification email.

**Root Cause:**
- SMTP credentials incorrect
- Firewall blocking SMTP port (587/465)
- Gmail "Less secure app access" turned off
- MailHog not running (in development)
- Email service throws exception silently

**Solution:**
1. Verify SMTP settings:
```bash
# Test SMTP connectivity
telnet smtp.gmail.com 587
```

2. Check application logs:
```csharp
// Add email-specific logging
builder.Logging.AddFilter("DainnUser.Infrastructure.Services.EmailService", LogLevel.Debug);
```

3. For Gmail: Use App Password (not account password)
   - Enable 2-Step Verification
   - Generate App Password at https://myaccount.google.com/apppasswords

4. For development: Use MailHog
```bash
# Start MailHog
docker run -d -p 1025:1025 -p 8025:8025 mailhog/mailhog
```
```json
{
  "DainnUser": {
    "Email": {
      "SmtpHost": "localhost",
      "SmtpPort": 1025,
      "SmtpUsername": "",
      "SmtpPassword": "",
      "FromEmail": "dev@localhost",
      "EnableSsl": false
    }
  }
}
```

**Prevention:** Monitor email service health. Add health check endpoint.

---

### Issue 11: Verification Token Invalid

**Symptom:** Clicking email verification link shows "Token may be invalid, expired, or already used."

**Root Cause:**
- Token expired (24-hour window)
- URL mangled by email client (line breaks, encoding)
- User already verified
- Token revoked (resend-verification was called after initial send)

**Solution:**
- User should call resend-verification endpoint:
```bash
curl -X POST https://localhost:5001/api/auth/resend-verification \
  -H "Content-Type: application/json" \
  -d '{"email": "user@example.com"}'
```
- Old tokens are revoked, new token is generated and sent
- Verify the link in the email was not broken across lines

**Prevention:** Include clear instructions in verification email. Add token expiration timestamp visible to users.

---

## OAuth / Social Login

### Issue 12: OAuth Callback Error (redirect_uri_mismatch)

**Symptom:** Google/Facebook OAuth shows "redirect_uri_mismatch" error.

**Root Cause:** Callback URL registered with OAuth provider does not match actual callback URL.

**Solution:**
1. Check the exact callback URL used:
```
https://yourdomain.com/signin-google
```

2. Register the exact URL in the OAuth provider console:
   - **Google:** https://console.cloud.google.com/apis/credentials
   - **Facebook:** https://developers.facebook.com/apps/
   - **GitHub:** https://github.com/settings/developers
   - **Microsoft:** https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps

3. Ensure HTTPS is configured and the domain matches exactly

**Prevention:** Use environment-specific OAuth apps (development vs production).

---

### Issue 13: OAuth Login Fails with 500

**Symptom:** `500 Internal Server Error` during social login.

**Root Cause:**
- OAuth credentials not configured or invalid
- `Enabled` flag not set to `true`
- UserService or SocialLoginService not registered

**Solution:**
```json
{
  "DainnUser": {
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret"
      }
    }
  }
}
```

Ensure social login is enabled in code:
```csharp
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    options.EnableSocialLogin = true;
});
```

**Prevention:** Test OAuth configuration by checking `/signin-{provider}` routes are registered at startup.

---

## Performance

### Issue 14: Slow User List Queries

**Symptom:** `/api/user` endpoint takes > 2 seconds with many users.

**Root Cause:** N+1 queries loading roles or profile data. Missing database indexes.

**Solution:**
Add database indexes:
```sql
-- Email index (already likely exists via unique constraint)
CREATE INDEX IX_Users_Status ON Users(Status);

-- Login history index
CREATE INDEX IX_LoginHistory_UserId ON LoginHistory(UserId, Timestamp);

-- Session index
CREATE INDEX IX_UserSessions_UserId ON UserSessions(UserId);
```

Optimize query code:
```csharp
// Bad: N+1 queries
var users = await _context.Users.ToListAsync();
foreach (var user in users)
{
    user.Roles = await _context.UserRoles.Where(r => r.UserId == user.Id).ToListAsync();
}

// Good: Eager loading
var users = await _context.Users
    .Include(u => u.Roles)
    .OrderBy(u => u.Username)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**Prevention:** Use eager loading, pagination, and database indexes.

---

### Issue 15: High Memory Usage Under Load

**Symptom:** Memory grows over time. GC pressure.

**Root Cause:**
- Entity tracking on read-only queries
- Large file uploads held in memory
- Missing `IAsyncEnumerable` for streaming

**Solution:**
```csharp
// Use AsNoTracking for read-only queries
var user = await _context.Users
    .AsNoTracking()
    .FirstOrDefaultAsync(u => u.Id == userId);

// Stream file uploads
app.Use(async (context, next) =>
{
    var maxRequestBodySize = app.Configuration
        .GetValue<long>("DainnUser:Storage:MaxFileSizeMB") * 1024 * 1024;
    context.Request.EnableBuffering(maxRequestBodySize);
    await next();
});
```

**Prevention:** Profile memory usage. Add GC.Collect() monitoring via `DOTNET_GCConserveMemory=9`.

---

## General Troubleshooting Checklist

1. **Check configuration:** `app.UseDainnUser()` called? `AddDainnUser()` registered?
2. **Check logs:** Enable `Information` level logging for DainnUser namespace
3. **Check connectivity:** Database, SMTP, OAuth providers reachable?
4. **Check migrations:** `dotnet ef database update` run?
5. **Check middleware order:** Authentication before Authorization
6. **Check HTTPS:** OAuth callbacks require HTTPS in production
7. **Check environment:** Correct `appsettings.{Environment}.json` loaded?
8. **Check JWT secret:** Same across all services? Min 32 characters?
9. **Check feature flags:** Required features enabled?
10. **Check NuGet version:** Compatible version installed?

---

## Logging

Enable detailed logging for troubleshooting:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DainnUser": "Debug",
      "DainnUser.Infrastructure": "Debug",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

For structured logging:
```csharp
_logger.LogWarning("Failed login attempt for user {Email} from {IpAddress}",
    email, HttpContext.Connection.RemoteIpAddress);
```

---

## See Also

- [Configuration Reference](configuration.md)
- [FAQ](faq.md)
- [Security Guide](security.md)
- [Known Issues](../../docs/known-issues.md)
