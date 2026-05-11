# PSA-70 Google OAuth Login Design

## Goal

Implement Google OAuth login using ASP.NET's built-in `Microsoft.AspNetCore.Authentication.Google` with an `ISocialLoginService` wrapper that enables login, account linking, and unlinking for external providers.

## Scope

In scope:

- Add `ISocialLoginService` interface with Google login, link, and unlink methods.
- Add `SocialLoginService` implementation using `GoogleHandler` for OAuth flow.
- Add `GoogleOAuthOptions` configuration block to `DainnUserOptions`.
- Auto-register new users on first Google login.
- Link Google account to existing authenticated user.
- Unlink external login providers.
- Generate JWT tokens (delegate to existing `IJwtTokenService` / `IAuthenticationService`).
- Unit and integration tests.

Out of scope:

- Facebook, GitHub, Microsoft OAuth (PSA-71, 72, 73).
- UI components for OAuth buttons (PSA-92).
- OAuth callback controller (already exists in `AuthController`).

## Existing Context

The codebase already has:

- `UserLogin` entity with Provider, ProviderKey, ProviderDisplayName, LinkedAt.
- `LoginProvider` enum with Google, Facebook, GitHub, Microsoft.
- `IUserRepository.GetByExternalLoginAsync(provider, providerKey, ct)`.
- `DainnUserOptions.EnableSocialLogin`.
- `AuthenticationService` with `LoginAsync` / `CompleteTwoFactorLoginAsync` returning `LoginResult`.

## Service Contract

Create `ISocialLoginService` in `DainnUser.Core.Interfaces.Services`:

```csharp
public interface ISocialLoginService
{
    Task<LoginResult> LoginWithGoogleAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default);

    Task LinkGoogleAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken ct = default);

    Task UnlinkProviderAsync(
        Guid userId,
        LoginProvider provider,
        CancellationToken ct = default);
}
```

## SocialLoginService Implementation

`SocialLoginService` lives in `DainnUser.Application.Services`, depends on `IUserRepository`, `IUnitOfWork`, `IJwtTokenService`, `ISessionService`, `DainnUserOptions`, and `HttpClient`.

**LoginWithGoogleAsync flow:**
1. Exchange `authorizationCode` for tokens via Google OAuth endpoint.
2. Fetch user info from `https://www.googleapis.com/oauth2/v3/userinfo`.
3. Look up existing `UserLogin` by `ProviderKey` (Google user ID).
4. If found → load the linked user, issue tokens, return `LoginResult`.
5. If not found by `ProviderKey` → look up `User` by email.
6. If user found by email → link Google account to existing user, issue tokens.
7. If no user at all → auto-register new user (with email verified), create `UserLogin`, issue tokens.

**LinkGoogleAccountAsync flow:**
1. Exchange code for tokens, fetch Google user info.
2. Verify user exists and Google account is not already linked to another user.
3. Create `UserLogin` for the authenticated user.
4. Persist via `IUnitOfWork`.

**UnlinkProviderAsync flow:**
1. Remove `UserLogin` for the user and provider.
2. Cannot unlink if it's the only login method (user has no password and no other external logins).
3. Persist via `IUnitOfWork`.

## Token Exchange

Exchange authorization code for tokens using Google's OAuth endpoint:

```
POST https://oauth2.googleapis.com/token
Body: code, client_id, client_secret, redirect_uri, grant_type=authorization_code
```

Fetch user info:
```
GET https://www.googleapis.com/oauth2/v3/userinfo
Header: Authorization: Bearer {access_token}
```

## Configuration

Add to `DainnUserOptions`:

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

**Auto-registration:** When enabled (`EnableSocialLogin = true`), users logging in with Google for the first time are auto-registered with a verified email. `EmailVerified = true` since Google already verified the account.

## Error Handling

- Invalid authorization code: throw `InvalidOperationException("Invalid Google authorization code.")`.
- Google account already linked to another user: throw `InvalidOperationException("Google account is already linked to another user.")`.
- Unlink last login method: throw `InvalidOperationException("Cannot unlink the only login method.")`.
- Social login disabled: throw `InvalidOperationException("Social login is not enabled.")`.

## Testing

Unit tests should cover:

- `LoginWithGoogleAsync` auto-registers new user.
- `LoginWithGoogleAsync` signs in existing user by `ProviderKey`.
- `LoginWithGoogleAsync` links Google account when email matches existing user.
- `LoginWithGoogleAsync` throws when social login disabled.
- `LinkGoogleAccountAsync` creates `UserLogin` for authenticated user.
- `UnlinkProviderAsync` removes `UserLogin`.
- `UnlinkProviderAsync` throws when unlinking last method.

Integration tests:

- Full Google OAuth login flow with mocked HTTP responses.
- Account linking end-to-end.

## Security and Compatibility

- No breaking changes to existing service contracts.
- Google secrets stored in `DainnUserOptions` (consumed via `IOptions<T>`).
- `SocialLoginService` is injectable and overridable via DI.
- No in-memory state.
- OAuth state/CSRF protection handled by ASP.NET middleware.
