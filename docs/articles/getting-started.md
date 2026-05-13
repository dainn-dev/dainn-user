# Getting Started with DainnUser

Quick start guide để integrate DainnUser library vào .NET application của bạn.

## Installation

### Via NuGet Package Manager

```bash
# Install meta-package (includes all components)
dotnet add package DainnUser

# Or install specific packages
dotnet add package DainnUser.Core
dotnet add package DainnUser.Infrastructure
dotnet add package DainnUser.Application
dotnet add package DainnUser.Api
dotnet add package DainnUser.Web
```

### Via Package Manager Console

```powershell
Install-Package DainnUser
```

## Basic Setup

### 1. Configure appsettings.json

```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=MyApp;Trusted_Connection=True;"
    },
    "Jwt": {
      "SecretKey": "your-secret-key-min-32-characters-long",
      "Issuer": "https://yourdomain.com",
      "Audience": "https://yourdomain.com",
      "ExpirationMinutes": 60
    },
    "Email": {
      "Provider": "Smtp",
      "SmtpHost": "smtp.gmail.com",
      "SmtpPort": 587,
      "SmtpUsername": "your-email@gmail.com",
      "SmtpPassword": "your-password",
      "FromEmail": "noreply@yourdomain.com",
      "FromName": "Your App"
    },
    "Security": {
      "RequireEmailVerification": true,
      "EnableTwoFactor": true,
      "PasswordRequirements": {
        "RequiredLength": 12
      }
    }
  }
}
```

### 2. Register Services in Program.cs

```csharp
using DainnUser.Api.Extensions;
using DainnUser.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add DainnUser services
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    // Optional: customize options
    options.EnableSocialLogin = true;
    options.EnableTwoFactor = true;
});

// Add controllers
builder.Services.AddControllers();

var app = builder.Build();

// Use DainnUser middleware
app.UseDainnUser();

// Standard middleware
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

### 3. Run Migrations

```bash
# Apply database migrations
dotnet ef database update --project src/YourApp --context DainnUserDbContext
```

## Quick Examples

### Register a New User

```csharp
[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AccountController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        
        if (result.Succeeded)
            return Ok(new { message = "Registration successful. Please check your email." });
        
        return BadRequest(result.Errors);
    }
}
```

### Login

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    var result = await _authService.LoginAsync(dto);
    
    if (result.Succeeded)
        return Ok(new 
        { 
            token = result.Token,
            refreshToken = result.RefreshToken,
            user = result.User
        });
    
    return Unauthorized(new { message = "Invalid credentials" });
}
```

### Get Current User Profile

```csharp
[Authorize]
[HttpGet("profile")]
public async Task<IActionResult> GetProfile()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var profile = await _profileService.GetProfileAsync(userId);
    
    return Ok(profile);
}
```

### Enable 2FA

```csharp
[Authorize]
[HttpPost("2fa/enable")]
public async Task<IActionResult> EnableTwoFactor()
{
    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
    var result = await _twoFactorService.EnableAsync(userId);
    
    return Ok(new 
    { 
        qrCodeUrl = result.QrCodeUrl,
        manualEntryKey = result.ManualEntryKey
    });
}
```

## Using with Razor Pages / MVC

### 1. Add DainnUser.Web Package

```bash
dotnet add package DainnUser.Web
```

### 2. Register Services

```csharp
builder.Services.AddDainnUser(builder.Configuration);
builder.Services.AddDainnUserWeb(); // Add web components
```

### 3. Use Components in Views

```razor
@using DainnUser.Web.Components

<DainnUser.Web.Components.LoginForm 
    OnSuccess="HandleLoginSuccess"
    EnableSocialLogin="true" />
```

## Using with Blazor

```razor
@page "/login"
@using DainnUser.Web.Components

<LoginForm 
    OnSuccess="@HandleLoginSuccess"
    EnableSocialLogin="true"
    EnableRememberMe="true" />

@code {
    private void HandleLoginSuccess(LoginResult result)
    {
        // Handle successful login
        NavigationManager.NavigateTo("/dashboard");
    }
}
```

## Configuration Options

### Enable/Disable Features

```json
{
  "DainnUser": {
    "Features": {
      "EnableRegistration": true,
      "EnableSocialLogin": true,
      "EnableTwoFactor": true,
      "EnableEmailVerification": true,
      "EnablePhoneVerification": false,
      "EnableRecaptcha": true
    }
  }
}
```

### Social Login Setup

```json
{
  "DainnUser": {
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret"
      },
      "Facebook": {
        "Enabled": true,
        "AppId": "your-facebook-app-id",
        "AppSecret": "your-facebook-app-secret"
      }
    }
  }
}
```

### Database Provider Setup

**SQL Server:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SqlServer",
      "ConnectionString": "Server=localhost;Database=MyApp;Trusted_Connection=True;"
    }
  }
}
```

**PostgreSQL:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "PostgreSQL",
      "ConnectionString": "Host=localhost;Database=myapp;Username=postgres;Password=password"
    }
  }
}
```

**MySQL:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "MySQL",
      "ConnectionString": "Server=localhost;Database=myapp;User=root;Password=password;"
    }
  }
}
```

**SQLite:**
```json
{
  "DainnUser": {
    "Database": {
      "Provider": "SQLite",
      "ConnectionString": "Data Source=myapp.db"
    }
  }
}
```

## Customization

### Override Default Services

```csharp
builder.Services.AddDainnUser(builder.Configuration);

// Override email service with custom implementation
builder.Services.AddScoped<IEmailService, MyCustomEmailService>();
```

### Extend User Entity

```csharp
public class MyApplicationUser : DainnUser.Core.Entities.User
{
    public string CompanyName { get; set; }
    public string Department { get; set; }
}

// Register custom user
builder.Services.AddDainnUser<MyApplicationUser>(builder.Configuration);
```

### Custom Validation Rules

```csharp
public class CustomRegisterValidator : AbstractValidator<RegisterDto>
{
    public CustomRegisterValidator()
    {
        // Add custom rules
        RuleFor(x => x.Email)
            .Must(BeCompanyEmail)
            .WithMessage("Only company emails are allowed");
    }
    
    private bool BeCompanyEmail(string email)
    {
        return email.EndsWith("@mycompany.com");
    }
}

// Register custom validator
builder.Services.AddScoped<IValidator<RegisterDto>, CustomRegisterValidator>();
```

## Next Steps

- [Configuration Guide](../configuration.md) — Detailed configuration options
- [API Reference](../api-reference.md) — Complete API documentation
- [Security Guide](security.md) — Security best practices
- [Migration Guide](migrations.md) — Database migrations guide
- [Samples](../../samples/) — Complete sample applications

## Troubleshooting

### Common Issues

**Issue: Database migrations not applying**
```bash
# Ensure you're targeting the correct project
dotnet ef database update --project src/YourApp --context DainnUserDbContext
```

**Issue: JWT authentication not working**
```csharp
// Ensure UseAuthentication() is called before UseAuthorization()
app.UseAuthentication();
app.UseAuthorization();
```

**Issue: Email not sending**
- Check SMTP credentials in appsettings.json
- Verify firewall allows SMTP port (587/465)
- Check email service logs

## Support

- GitHub Issues: https://github.com/yourorg/dainnuser/issues
- Documentation: https://docs.dainnuser.com
- Email: support@dainnuser.com
