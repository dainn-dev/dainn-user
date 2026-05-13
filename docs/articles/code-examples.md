# Code Examples — DainnUser

Cookbook scenarios with complete C# examples.

> Examples assume DainnUser services are registered with `builder.Services.AddDainnUser(builder.Configuration);` and middleware is configured with `app.UseDainnUser();`.

---

## Common Setup

Use this setup for controller examples.

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using DainnUser.Api.DTOs;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDainnUser(builder.Configuration, options =>
{
    options.EnableSocialLogin = true;
    options.EnableTwoFactor = true;
    options.RequireEmailVerification = true;
    options.EnableAccountLockout = true;
    options.EnableSessionManagement = true;
    options.EnableActivityLogging = true;
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseDainnUser();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

---

## 1. Register User and Verify Email

Complete registration and email verification flow.

```csharp
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/account")]
public class AccountRegistrationController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AccountRegistrationController> _logger;

    public AccountRegistrationController(
        IAuthenticationService authenticationService,
        ILogger<AccountRegistrationController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Passwords do not match." });
        }

        try
        {
            var userId = await _authenticationService.RegisterAsync(
                request.Email,
                request.Username,
                request.Password,
                cancellationToken);

            _logger.LogInformation("User registered: {UserId}", userId);

            return Created($"/api/users/{userId}", new
            {
                userId,
                message = "Registration successful. Please check your email to verify your account."
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        var verified = await _authenticationService.VerifyEmailAsync(
            request.UserId,
            request.Token,
            cancellationToken);

        if (!verified)
        {
            return BadRequest(new
            {
                message = "Email verification failed. Token may be invalid, expired, or already used."
            });
        }

        return Ok(new { message = "Email verified successfully." });
    }

    [HttpPost("resend-verification")]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var sent = await _authenticationService.ResendVerificationEmailAsync(
            request.Email,
            cancellationToken);

        if (!sent)
        {
            return BadRequest(new
            {
                message = "Failed to resend verification email. User may not exist or email is already verified."
            });
        }

        return Ok(new { message = "Verification email sent." });
    }
}

public sealed record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string ConfirmPassword);

public sealed record VerifyEmailRequest(Guid UserId, string Token);
public sealed record ResendVerificationRequest(string Email);
```

---

## 2. Login and Store Tokens

API controller login plus typed client token handling.

```csharp
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/account")]
public class LoginController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public LoginController(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.LoginAsync(
            request.Email,
            request.Password,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        if (result.RequiresTwoFactor)
        {
            return Ok(new
            {
                requiresTwoFactor = true,
                userId = result.User.Id,
                message = "Two-factor authentication required."
            });
        }

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresIn = result.ExpiresIn,
            tokenType = "Bearer",
            user = result.User
        });
    }
}

public sealed record LoginRequest(string Email, string Password);

public class DainnUserApiClient
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private string? _refreshToken;
    private DateTime _accessTokenExpiresAt;

    public DainnUserApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LoginAsync(string email, string password)
    {
        var response = await _httpClient.PostAsJsonAsync("api/auth/login", new
        {
            email,
            password
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        if (result?.Data is null)
        {
            throw new InvalidOperationException("Login response was empty.");
        }

        _accessToken = result.Data.AccessToken;
        _refreshToken = result.Data.RefreshToken;
        _accessTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.Data.ExpiresIn);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
    }
}
```

---

## 3. Refresh Tokens Automatically

Use `DelegatingHandler` to refresh access tokens before expiry.

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public interface ITokenStore
{
    string? AccessToken { get; set; }
    string? RefreshToken { get; set; }
    DateTime AccessTokenExpiresAt { get; set; }
}

public class InMemoryTokenStore : ITokenStore
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime AccessTokenExpiresAt { get; set; }
}

public class RefreshTokenHandler : DelegatingHandler
{
    private readonly ITokenStore _tokenStore;
    private readonly IHttpClientFactory _httpClientFactory;

    public RefreshTokenHandler(ITokenStore tokenStore, IHttpClientFactory httpClientFactory)
    {
        _tokenStore = tokenStore;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_tokenStore.AccessToken) &&
            _tokenStore.AccessTokenExpiresAt <= DateTime.UtcNow.AddMinutes(5))
        {
            await RefreshAccessTokenAsync(cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized &&
            !string.IsNullOrWhiteSpace(_tokenStore.RefreshToken))
        {
            await RefreshAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
            response.Dispose();
            return await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    private async Task RefreshAccessTokenAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("DainnUserAnonymous");
        var response = await client.PostAsJsonAsync("api/auth/refresh-token", new
        {
            refreshToken = _tokenStore.RefreshToken
        }, cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(cancellationToken);
        if (result?.Data is null)
        {
            throw new InvalidOperationException("Refresh token response was empty.");
        }

        _tokenStore.AccessToken = result.Data.AccessToken;
        _tokenStore.RefreshToken = result.Data.RefreshToken; // Store rotated refresh token
        _tokenStore.AccessTokenExpiresAt = DateTime.UtcNow.AddSeconds(result.Data.ExpiresIn);
    }
}

// Program.cs
builder.Services.AddSingleton<ITokenStore, InMemoryTokenStore>();
builder.Services.AddTransient<RefreshTokenHandler>();
builder.Services.AddHttpClient("DainnUserAnonymous", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/");
});
builder.Services.AddHttpClient("DainnUser", client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/");
}).AddHttpMessageHandler<RefreshTokenHandler>();
```

---

## 4. Social Login (Google)

Configure Google OAuth and expose endpoints.

```csharp
// appsettings.json
/*
{
  "DainnUser": {
    "Features": {
      "EnableSocialLogin": true
    },
    "OAuth": {
      "Google": {
        "Enabled": true,
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret",
        "CallbackPath": "/signin-google"
      }
    }
  }
}
*/

using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/social")]
public class SocialLoginController : ControllerBase
{
    private readonly ISocialLoginService _socialLoginService;

    public SocialLoginController(ISocialLoginService socialLoginService)
    {
        _socialLoginService = socialLoginService;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = "/")
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl })
        };

        return Challenge(properties, "Google");
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string? returnUrl,
        CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync("Google");
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Google authentication failed." });
        }

        var email = result.Principal?.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal?.FindFirst(ClaimTypes.Name)?.Value;
        var providerKey = result.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(providerKey))
        {
            return BadRequest(new { message = "Google account did not provide required claims." });
        }

        var loginResult = await _socialLoginService.LoginAsync(
            provider: "Google",
            providerKey: providerKey,
            email: email,
            displayName: name,
            cancellationToken: cancellationToken);

        return Ok(new
        {
            accessToken = loginResult.AccessToken,
            refreshToken = loginResult.RefreshToken,
            expiresIn = loginResult.ExpiresIn,
            returnUrl = returnUrl ?? "/"
        });
    }
}
```

---

## 5. Password Reset Flow

Forgot-password and reset-password endpoints.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/account")]
public class PasswordResetController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<PasswordResetController> _logger;

    public PasswordResetController(
        IAuthenticationService authenticationService,
        ILogger<PasswordResetController> logger)
    {
        _authenticationService = authenticationService;
        _logger = logger;
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        // Always returns OK to prevent user enumeration.
        await _authenticationService.ForgotPasswordAsync(request.Email, cancellationToken);

        _logger.LogInformation("Password reset requested for {Email}", request.Email);

        return Ok(new
        {
            message = "If an account with that email exists, a password reset link has been sent."
        });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (request.NewPassword != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Passwords do not match." });
        }

        var reset = await _authenticationService.ResetPasswordAsync(
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!reset)
        {
            return BadRequest(new { message = "Invalid or expired password reset token." });
        }

        return Ok(new
        {
            message = "Password reset successfully. All active sessions have been invalidated."
        });
    }
}

public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);
```

---

## 6. Two-Factor Authentication Setup

Setup, enable, and complete 2FA login.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/account/2fa")]
public class TwoFactorController : ControllerBase
{
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuthenticationService _authenticationService;

    public TwoFactorController(
        ITwoFactorService twoFactorService,
        IAuthenticationService authenticationService)
    {
        _twoFactorService = twoFactorService;
        _authenticationService = authenticationService;
    }

    [Authorize]
    [HttpPost("setup")]
    public async Task<IActionResult> Setup(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _twoFactorService.SetupAsync(userId, cancellationToken);

        return Ok(new
        {
            secret = result.Secret,
            qrCodeUri = result.QrCodeUri,
            message = "Scan QR code with your authenticator app and confirm with a code."
        });
    }

    [Authorize]
    [HttpPost("enable")]
    public async Task<IActionResult> Enable(
        [FromBody] TwoFactorCodeRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var backupCodes = await _twoFactorService.EnableAsync(userId, request.Code, cancellationToken);

        return Ok(new
        {
            backupCodes,
            message = "Two-factor authentication enabled. Store backup codes securely."
        });
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> CompleteLogin(
        [FromBody] CompleteTwoFactorLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authenticationService.CompleteTwoFactorLoginAsync(
            request.UserId,
            request.Code,
            request.RememberDevice,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresIn = result.ExpiresIn,
            tokenType = "Bearer"
        });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId)
            ? userId
            : throw new UnauthorizedAccessException("Invalid user identifier.");
    }
}

public sealed record TwoFactorCodeRequest(string Code);
public sealed record CompleteTwoFactorLoginRequest(Guid UserId, string Code, bool RememberDevice);
```

---

## 7. Session Management

List, revoke one, or revoke all user sessions.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Authorize]
[Route("api/sessions")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var sessions = await _sessionService.GetActiveSessionsAsync(userId, cancellationToken);

        return Ok(sessions.Select(session => new
        {
            session.Id,
            session.IpAddress,
            session.UserAgent,
            session.CreatedAt,
            session.LastActivityAt,
            session.ExpiresAt
        }));
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> Revoke(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _sessionService.RevokeAsync(sessionId, userId, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> RevokeAll(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _sessionService.RevokeAllAsync(userId, cancellationToken);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}
```

---

## 8. Avatar Upload

Secure avatar upload with size and content-type validation.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Authorize]
[Route("api/profile/avatar")]
public class AvatarController : ControllerBase
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/webp"
    };

    private readonly IAvatarService _avatarService;

    public AvatarController(IAvatarService avatarService)
    {
        _avatarService = avatarService;
    }

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "File is required." });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { message = "File size must not exceed 5MB." });
        }

        if (!AllowedContentTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Invalid file type. Only JPEG, PNG, GIF, and WebP are allowed." });
        }

        await using var stream = file.OpenReadStream();
        var userId = GetCurrentUserId();
        var avatarUrl = await _avatarService.UploadAsync(
            userId,
            stream,
            file.FileName,
            file.ContentType,
            cancellationToken);

        return Ok(new
        {
            avatarUrl,
            message = "Avatar uploaded successfully."
        });
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _avatarService.DeleteAsync(userId, cancellationToken);
        return Ok(new { message = "Avatar deleted successfully." });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}
```

---

## 9. Admin Create User

Administrator creates user and assigns roles.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin/users")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly IRoleService _roleService;
    private readonly ILogger<AdminUsersController> _logger;

    public AdminUsersController(
        IUserManagementService userManagementService,
        IRoleService roleService,
        ILogger<AdminUsersController> logger)
    {
        _userManagementService = userManagementService;
        _roleService = roleService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] AdminCreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
        {
            return BadRequest(new { message = "Passwords do not match." });
        }

        var user = await _userManagementService.CreateUserAsync(
            request.Email,
            request.Username,
            request.Password,
            emailVerified: request.EmailVerified,
            cancellationToken);

        foreach (var roleName in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            await _roleService.AddUserToRoleAsync(user.Id, roleName, cancellationToken);
        }

        _logger.LogInformation("Administrator {AdminId} created user {UserId}",
            User.Identity?.Name,
            user.Id);

        return Created($"/api/admin/users/{user.Id}", new
        {
            user.Id,
            user.Email,
            user.Username,
            roles = request.Roles,
            message = "User created successfully."
        });
    }
}

public sealed record AdminCreateUserRequest(
    string Email,
    string Username,
    string Password,
    string ConfirmPassword,
    bool EmailVerified,
    IReadOnlyList<string> Roles);
```

---

## 10. Admin Lock and Unlock User

Administrative account lock and unlock endpoints.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Authorize(Roles = "Administrator")]
[Route("api/admin/users")]
public class AdminLockController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<AdminLockController> _logger;

    public AdminLockController(
        IUserManagementService userManagementService,
        ILogger<AdminLockController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    [HttpPost("{userId:guid}/lock")]
    public async Task<IActionResult> Lock(
        Guid userId,
        [FromBody] LockUserRequest request,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (userId == currentUserId)
        {
            return BadRequest(new { message = "Cannot lock your own account." });
        }

        await _userManagementService.LockUserAsync(
            userId,
            request.Reason,
            cancellationToken);

        _logger.LogWarning("Administrator {AdminId} locked user {UserId}. Reason: {Reason}",
            currentUserId,
            userId,
            request.Reason);

        return Ok(new { message = "User locked successfully." });
    }

    [HttpPost("{userId:guid}/unlock")]
    public async Task<IActionResult> Unlock(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _userManagementService.UnlockUserAsync(userId, cancellationToken);

        _logger.LogInformation("Administrator {AdminId} unlocked user {UserId}",
            GetCurrentUserId(),
            userId);

        return Ok(new { message = "User unlocked successfully." });
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}

public sealed record LockUserRequest(string Reason);
```

---

## 11. Custom Registration Validation

Restrict registration to company emails and validate username policy.

```csharp
using DainnUser.Application.DTOs.Authentication;
using FluentValidation;

public class CompanyRegisterValidator : AbstractValidator<RegisterDto>
{
    public CompanyRegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255)
            .Must(BeCompanyEmail)
            .WithMessage("Only @mycompany.com email addresses are allowed.");

        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z][a-zA-Z0-9_-]*$")
            .WithMessage("Username must start with a letter and contain only letters, numbers, underscore, or dash.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");
    }

    private static bool BeCompanyEmail(string email)
    {
        return email.EndsWith("@mycompany.com", StringComparison.OrdinalIgnoreCase);
    }
}

// Program.cs: replace default validator
builder.Services.AddScoped<IValidator<RegisterDto>, CompanyRegisterValidator>();
```

---

## 12. Custom Email Service

Replace default SMTP email service with SendGrid or custom provider.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;

public class SendGridEmailOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "DainnUser";
}

public class SendGridEmailService : IEmailService
{
    private readonly ISendGridClient _client;
    private readonly SendGridEmailOptions _options;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClient client,
        IOptions<SendGridEmailOptions> options,
        ILogger<SendGridEmailService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default)
    {
        var from = new EmailAddress(_options.FromEmail, _options.FromName);
        var recipient = new EmailAddress(to);
        var message = MailHelper.CreateSingleEmail(
            from,
            recipient,
            subject,
            plainTextContent: null,
            htmlContent: htmlBody);

        var response = await _client.SendEmailAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SendGrid failed with status {StatusCode}: {Body}",
                response.StatusCode,
                body);
            throw new InvalidOperationException("Email delivery failed.");
        }
    }
}

// Program.cs
builder.Services.Configure<SendGridEmailOptions>(
    builder.Configuration.GetSection("DainnUser:Email:SendGrid"));
builder.Services.AddSingleton<ISendGridClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<SendGridEmailOptions>>().Value;
    return new SendGridClient(options.ApiKey);
});
builder.Services.AddScoped<IEmailService, SendGridEmailService>();
```

---

## 13. Custom User Entity

Extend default user with business fields.

```csharp
using DainnUser.Core.Entities;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MyAppUser : User
{
    public string? CompanyName { get; set; }
    public string? Department { get; set; }
    public DateTime? HireDate { get; set; }
    public string? EmployeeId { get; set; }
}

public class MyAppUserConfiguration : IEntityTypeConfiguration<MyAppUser>
{
    public void Configure(EntityTypeBuilder<MyAppUser> builder)
    {
        builder.Property(x => x.CompanyName)
            .HasMaxLength(200);

        builder.Property(x => x.Department)
            .HasMaxLength(100);

        builder.Property(x => x.EmployeeId)
            .HasMaxLength(50);

        builder.HasIndex(x => x.EmployeeId)
            .IsUnique()
            .HasFilter("EmployeeId IS NOT NULL");
    }
}

// Program.cs
builder.Services.AddDainnUser<MyAppUser>(builder.Configuration);
```

---

## 14. Activity Audit Query

Show recent user security activity.

```csharp
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Authorize]
[Route("api/activity")]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;

    public ActivityController(IActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecentActivity(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        days = Math.Clamp(days, 1, 90);
        var userId = GetCurrentUserId();

        var activities = await _activityService.GetActivityLogAsync(
            userId,
            startDate: DateTime.UtcNow.AddDays(-days),
            endDate: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        return Ok(activities.Select(activity => new
        {
            activity.Id,
            activity.Action,
            activity.IpAddress,
            activity.UserAgent,
            activity.CreatedAt
        }));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}
```

---

## See Also

- [Getting Started](getting-started.md)
- [API Endpoints](api-endpoints.md)
- [Configuration Reference](configuration.md)
- [Security Guide](security.md)
- [Troubleshooting](troubleshooting.md)
