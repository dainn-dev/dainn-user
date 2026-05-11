# PSA-70 Google OAuth Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Google OAuth login via `ISocialLoginService` wrapping ASP.NET `GoogleHandler`, with auto-registration, account linking, and unlinking.

**Architecture:** SocialLoginService calls Google's OAuth API via HttpClient, creates/finds users, generates tokens by delegating to AuthenticationService. Follows existing service/interface/DI patterns.

**Tech Stack:** .NET 8, Microsoft.AspNetCore.Authentication.Google, xUnit, Moq, FluentAssertions

---

## File Map

```
src/DainnUser.Core/Interfaces/Services/
  + ISocialLoginService.cs          ← new

src/DainnUser.Application/Services/
  + SocialLoginService.cs           ← new

src/DainnUser.Application/
  ApplicationServiceExtensions.cs   ← modify (add DI registration)

src/DainnUser.Infrastructure/Configuration/
  DainnUserOptions.cs              ← modify (add Google OAuth options)

tests/DainnUser.UnitTests/Services/
  + SocialLoginServiceTests.cs     ← new

tests/DainnUser.IntegrationTests/Services/
  + SocialLoginServiceIntegrationTests.cs ← new
```

---

## Task 1: Add Google OAuth options to DainnUserOptions

**Files:**
- Modify: `src/DainnUser.Infrastructure/Configuration/DainnUserOptions.cs`

Add after `LoginHistoryRetentionDays` property:

```csharp
/// <summary>
/// Gets or sets the Google OAuth client ID.
/// </summary>
public string GoogleClientId { get; set; } = string.Empty;

/// <summary>
/// Gets or sets the Google OAuth client secret.
/// </summary>
public string GoogleClientSecret { get; set; } = string.Empty;

/// <summary>
/// Gets or sets the Google OAuth callback path.
/// </summary>
public string GoogleCallbackPath { get; set; } = "/signin-google";
```

---

## Task 2: Create ISocialLoginService interface

**Files:**
- Create: `src/DainnUser.Core/Interfaces/Services/ISocialLoginService.cs`

```csharp
using DainnUser.Core.Enums;
using DainnUser.Core.Models.Authentication;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service interface for social login providers (Google, Facebook, GitHub, Microsoft).
/// </summary>
public interface ISocialLoginService
{
    /// <summary>
    /// Authenticates a user via Google OAuth authorization code flow.
    /// Auto-registers new users if no existing account is found.
    /// </summary>
    Task<LoginResult> LoginWithGoogleAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Links a Google account to an already-authenticated user.
    /// </summary>
    Task LinkGoogleAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlinks an external login provider from a user account.
    /// The user must have at least one other login method (password or another provider).
    /// </summary>
    Task UnlinkProviderAsync(
        Guid userId,
        LoginProvider provider,
        CancellationToken cancellationToken = default);
}
```

---

## Task 3: Create SocialLoginService implementation

**Files:**
- Create: `src/DainnUser.Application/Services/SocialLoginService.cs`

```csharp
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using DainnUser.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for social login providers.
/// </summary>
public class SocialLoginService : ISocialLoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService? _sessionService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IAuthenticationService _authenticationService;
    private readonly DainnUserOptions _options;
    private readonly HttpClient _httpClient;

    public SocialLoginService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ISessionService? sessionService,
        IPasswordHasher<User> passwordHasher,
        IAuthenticationService authenticationService,
        IOptions<DainnUserOptions> options,
        HttpClient httpClient)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _passwordHasher = passwordHasher;
        _authenticationService = authenticationService;
        _options = options.Value;
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginWithGoogleAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var (accessToken, _) = await ExchangeCodeForTokensAsync(authorizationCode, callbackUrl, ct);
        var googleUser = await FetchGoogleUserInfoAsync(accessToken, ct);

        // Try to find existing user by provider key first
        var user = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Google, googleUser.Id, ct);

        if (user is not null)
        {
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        // Look up by email
        user = await _userRepository.GetByEmailAsync(googleUser.Email, ct);
        if (user is not null)
        {
            // Link Google account to existing user
            await CreateUserLoginAsync(user.Id, googleUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        // Auto-register new user
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = googleUser.Email,
            Username = DeriveUsername(googleUser),
            EmailVerified = true, // Google already verified
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Generate a random password hash so password login is disabled
        user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

        await _userRepository.AddAsync(user, ct);
        await CreateUserLoginAsync(user.Id, googleUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await IssueLoginResult(user, ipAddress, userAgent, ct);
    }

    /// <inheritdoc/>
    public async Task LinkGoogleAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        var (accessToken, _) = await ExchangeCodeForTokensAsync(authorizationCode, callbackUrl, ct);
        var googleUser = await FetchGoogleUserInfoAsync(accessToken, ct);

        // Check if this Google account is already linked to someone else
        var existingUser = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Google, googleUser.Id, ct);
        if (existingUser is not null)
        {
            if (existingUser.Id != userId)
                throw new InvalidOperationException("Google account is already linked to another user.");
            return; // Already linked to this user
        }

        await CreateUserLoginAsync(userId, googleUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UnlinkProviderAsync(
        Guid userId,
        LoginProvider provider,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        var login = user.Logins?.FirstOrDefault(l => l.Provider == provider);
        if (login is null)
            return; // Already unlinked

        // Don't allow unlinking if it's the only login method
        var hasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);
        var externalLoginCount = user.Logins?.Count(l => l.Provider != provider) ?? 0;

        if (!hasPassword && externalLoginCount == 0)
            throw new InvalidOperationException("Cannot unlink the only login method.");

        user.Logins!.Remove(login);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // --- helpers ---

    private void EnsureSocialLoginEnabled()
    {
        if (!_options.EnableSocialLogin)
            throw new InvalidOperationException("Social login is not enabled.");
    }

    private async Task<(string AccessToken, string RefreshToken)> ExchangeCodeForTokensAsync(
        string code, string callbackUrl, CancellationToken ct)
    {
        var payload = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.GoogleClientId,
            ["client_secret"] = _options.GoogleClientSecret,
            ["redirect_uri"] = callbackUrl,
            ["grant_type"] = "authorization_code"
        };

        var response = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(payload), ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Invalid Google authorization code.");

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(
            cancellationToken: ct);

        if (tokenResponse?.AccessToken is null)
            throw new InvalidOperationException("Invalid Google authorization code.");

        return (tokenResponse.AccessToken, tokenResponse.RefreshToken ?? string.Empty);
    }

    private async Task<GoogleUserInfo> FetchGoogleUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync(
            "https://www.googleapis.com/oauth2/v3/userinfo", ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch Google user information.");

        var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken: ct);
        return userInfo ?? throw new InvalidOperationException("Failed to parse Google user information.");
    }

    private async Task CreateUserLoginAsync(Guid userId, GoogleUserInfo googleUser, CancellationToken ct)
    {
        await _unitOfWork.Users.AddLoginAsync(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Google,
            ProviderKey = googleUser.Id,
            ProviderDisplayName = googleUser.Name,
            LinkedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task<LoginResult> IssueLoginResult(
        User user, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        // Reuse AuthenticationService for consistent login flow.
        // Since social login users have no password, we delegate to
        // AuthenticationService with existing user identity.
        // Directly generate tokens ourselves for social-auth users.
        var roleNames = user.UserRoles?
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .ToList() ?? new List<string>();

        var permissions = user.UserRoles?
            .Select(ur => ur.Role?.Permissions)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .SelectMany(p => p!.Split(',').Select(x => x.Trim().ToLowerInvariant()))
            .Distinct()
            .ToList() ?? new List<string>();

        var sessionId = Guid.NewGuid();
        var accessToken = permissions.Count == 0
            ? _jwtTokenService.GenerateAccessToken(user, roleNames, sessionId)
            : _jwtTokenService.GenerateAccessToken(user, roleNames, permissions, sessionId);

        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.RefreshToken,
            TokenValue = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, ct);

        if (_options.EnableSessionManagement && _sessionService is not null)
        {
            var session = await _sessionService.CreateSessionAsync(
                user.Id, refreshTokenHash, ipAddress, userAgent, ct);
            sessionId = session.Id;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return new LoginResult
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            SessionId = sessionId,
            RequiresTwoFactor = false,
            User = new AuthenticatedUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                EmailVerified = user.EmailVerified,
                Roles = roleNames
            }
        };
    }

    private static string DeriveUsername(GoogleUserInfo googleUser)
    {
        // Use email prefix + random suffix if needed
        var prefix = googleUser.Email.Split('@')[0];
        return prefix;
    }

    // --- Google API models ---

    private class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }
}
```

Note: `IUserRepository` needs `AddLoginAsync(UserLogin login, CancellationToken)`. Add if missing.

---

## Task 4: Add AddLoginAsync to IUserRepository/UserRepository (if needed)

**Files:**
- Modify: `src/DainnUser.Core/Interfaces/Repositories/IUserRepository.cs`
- Modify: `src/DainnUser.Infrastructure/Repositories/UserRepository.cs`

Add to `IUserRepository`:

```csharp
/// <summary>
/// Adds a <see cref="UserLogin"/> for the user.
/// </summary>
Task AddLoginAsync(UserLogin login, CancellationToken cancellationToken = default);
```

Add to `UserRepository`:

```csharp
public async Task AddLoginAsync(UserLogin login, CancellationToken ct = default)
{
    await _context.Set<UserLogin>().AddAsync(login, ct);
}
```

---

## Task 5: Register SocialLoginService in DI

**Files:**
- Modify: `src/DainnUser.Application/ApplicationServiceExtensions.cs`

Add alongside other service registrations:

```csharp
services.AddScoped<ISocialLoginService, SocialLoginService>();
```

---

## Task 6: Write unit tests for SocialLoginService

**Files:**
- Create: `tests/DainnUser.UnitTests/Services/SocialLoginServiceTests.cs`

Test cases (8):
1. `LoginWithGoogleAsync` auto-registers new user when no existing match.
2. `LoginWithGoogleAsync` signs in existing user by ProviderKey match.
3. `LoginWithGoogleAsync` links Google account when email matches existing user.
4. `LoginWithGoogleAsync` throws when social login disabled.
5. `LoginWithGoogleAsync` throws when authorization code is invalid.
6. `LinkGoogleAccountAsync` creates UserLogin for authenticated user.
7. `LinkGoogleAccountAsync` throws when Google account already linked to another user.
8. `UnlinkProviderAsync` removes UserLogin.
9. `UnlinkProviderAsync` throws when unlinking last login method.

Mock `ILoginHistoryRepository`, `ILoginHistoryService`, `ISessionService` and dependencies via Moq. Mock `HttpClient` responses for token exchange and user info.

---

## Task 7: Write integration tests for SocialLoginService

**Files:**
- Create: `tests/DainnUser.IntegrationTests/Services/SocialLoginServiceIntegrationTests.cs`

Use existing DatabaseFixture. Mock HTTP responses.

Test cases (4):
1. New user auto-registers via Google OAuth and login succeeds.
2. Existing user by email gets Google account linked.
3. Link Google account to authenticated user.
4. Unlink provider removes the login.

---

## Verification

After all tasks:
1. Run: `dotnet build` — must pass with no errors.
2. Run: `dotnet test` — all tests must pass.

---

## Spec Coverage Check

| Requirement | Task |
|---|---|
| `ISocialLoginService` interface | Task 2 |
| `LoginWithGoogleAsync()` | Tasks 2, 3 |
| `LinkGoogleAccountAsync()` | Tasks 2, 3 |
| `UnlinkProviderAsync()` | Tasks 2, 3 |
| Google OAuth configuration | Task 1 |
| Auto-register new user | Task 3 |
| Link Google to existing user | Task 3 |
| Store Google user ID | Task 3 |
| Fetch user info from Google | Task 3 |
| Enable/disable via configuration | Task 3 (EnsureSocialLoginEnabled) |
| `AddLoginAsync` repository method | Task 4 |
| DI registration | Task 5 |
| Unit tests | Task 6 |
| Integration tests | Task 7 |
