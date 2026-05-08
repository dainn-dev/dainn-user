# Security Review Skill

Dùng khi review code cho OWASP Top 10 compliance và security best practices.

## OWASP Top 10 (2021) Checklist

### A01: Broken Access Control

**Kiểm tra:**
- [ ] Authorization checks trên tất cả endpoints
- [ ] RBAC implementation đúng
- [ ] Không có direct object reference vulnerabilities
- [ ] User không thể access resources của user khác
- [ ] Admin endpoints có proper authorization

**Code Review:**
```csharp
// Good
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteUser(int id)
{
    // Additional check: ensure user can only delete if authorized
    if (!await _authService.CanDeleteUserAsync(User, id))
        return Forbid();
    
    await _userService.DeleteAsync(id);
    return NoContent();
}

// Bad - missing authorization
public async Task<IActionResult> DeleteUser(int id)
{
    await _userService.DeleteAsync(id);
    return NoContent();
}
```

### A02: Cryptographic Failures

**Kiểm tra:**
- [ ] Passwords được hash với strong algorithm (Argon2, PBKDF2)
- [ ] Sensitive data encrypted at rest
- [ ] TLS/HTTPS enforced
- [ ] Không có hardcoded secrets
- [ ] Secure random number generation

**Code Review:**
```csharp
// Good
var hasher = new PasswordHasher<User>();
user.PasswordHash = hasher.HashPassword(user, password);

// Bad - plain text password
user.Password = password; // NEVER DO THIS
```

### A03: Injection

**Kiểm tra:**
- [ ] Parameterized queries (EF Core handles this)
- [ ] Input validation trên tất cả user inputs
- [ ] No dynamic SQL construction
- [ ] Command injection prevention
- [ ] LDAP injection prevention

**Code Review:**
```csharp
// Good - EF Core parameterized
var user = await _context.Users
    .Where(u => u.Email == email)
    .FirstOrDefaultAsync();

// Bad - string interpolation (EF Core still safe, but avoid)
var user = await _context.Users
    .FromSqlRaw($"SELECT * FROM Users WHERE Email = '{email}'") // Vulnerable if not using EF Core
    .FirstOrDefaultAsync();
```

### A04: Insecure Design

**Kiểm tra:**
- [ ] Threat modeling done
- [ ] Secure by default configuration
- [ ] Rate limiting implemented
- [ ] Account lockout after failed attempts
- [ ] Session timeout configured

**Code Review:**
```csharp
// Good - rate limiting
[RateLimit(MaxRequests = 5, WindowSeconds = 60)]
public async Task<IActionResult> Login(LoginDto dto)
{
    // ...
}
```

### A05: Security Misconfiguration

**Kiểm tra:**
- [ ] No default credentials
- [ ] Error messages không leak sensitive info
- [ ] Security headers configured (HSTS, CSP, X-Frame-Options)
- [ ] Unnecessary features disabled
- [ ] Dependencies up to date

**Code Review:**
```csharp
// Good - generic error message
catch (Exception ex)
{
    _logger.LogError(ex, "Login failed for user {Email}", email);
    return Unauthorized(new { message = "Invalid credentials" });
}

// Bad - leaking info
catch (Exception ex)
{
    return BadRequest(new { message = ex.Message, stackTrace = ex.StackTrace });
}
```

### A06: Vulnerable and Outdated Components

**Kiểm tra:**
- [ ] All NuGet packages up to date
- [ ] No known vulnerabilities in dependencies
- [ ] Regular dependency audits

**Commands:**
```bash
# Check for vulnerable packages
dotnet list package --vulnerable

# Update packages
dotnet add package PackageName
```

### A07: Identification and Authentication Failures

**Kiểm tra:**
- [ ] Strong password policy
- [ ] 2FA available
- [ ] Session management secure
- [ ] Credential stuffing protection
- [ ] Brute force protection

**Code Review:**
```csharp
// Good - password requirements
services.Configure<IdentityOptions>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 12;
    
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
});
```

### A08: Software and Data Integrity Failures

**Kiểm tra:**
- [ ] NuGet packages signed
- [ ] CI/CD pipeline secure
- [ ] No unsigned code execution
- [ ] Integrity checks on critical data

### A09: Security Logging and Monitoring Failures

**Kiểm tra:**
- [ ] Login attempts logged
- [ ] Failed authorization logged
- [ ] Sensitive operations audited
- [ ] No sensitive data in logs (passwords, tokens)
- [ ] Log tampering prevention

**Code Review:**
```csharp
// Good - structured logging without sensitive data
_logger.LogInformation("User {UserId} logged in from {IpAddress}", 
    user.Id, httpContext.Connection.RemoteIpAddress);

// Bad - logging sensitive data
_logger.LogInformation("User logged in with password {Password}", password);
```

### A10: Server-Side Request Forgery (SSRF)

**Kiểm tra:**
- [ ] URL validation on external requests
- [ ] Whitelist allowed domains
- [ ] No user-controlled URLs without validation
- [ ] Network segmentation

## Additional Security Checks

### Input Validation

```csharp
// Use FluentValidation
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
            .Matches(@"[A-Z]").WithMessage("Password must contain uppercase")
            .Matches(@"[a-z]").WithMessage("Password must contain lowercase")
            .Matches(@"[0-9]").WithMessage("Password must contain digit")
            .Matches(@"[\W]").WithMessage("Password must contain special character");
    }
}
```

### CSRF Protection

```csharp
// Enable anti-forgery tokens
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});
```

### Secure Cookies

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

## Security Testing

```bash
# Run security tests
dotnet test tests/DainnUser.SecurityTests/

# Static analysis
dotnet tool install --global security-scan
security-scan DainnUser.sln

# Dependency check
dotnet list package --vulnerable --include-transitive
```

## Security Review Checklist

- [ ] OWASP Top 10 addressed
- [ ] Input validation complete
- [ ] Authentication secure
- [ ] Authorization enforced
- [ ] Sensitive data protected
- [ ] Logging appropriate (no secrets)
- [ ] Error handling secure
- [ ] Dependencies up to date
- [ ] Security tests passing
- [ ] Code review done
