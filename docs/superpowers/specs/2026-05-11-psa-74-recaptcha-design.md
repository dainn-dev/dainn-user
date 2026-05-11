# PSA-74 Google reCAPTCHA Integration Design

## Goal

Implement Google reCAPTCHA verification service (`IRecaptchaService`) to protect registration, login, and password reset endpoints from bots. Support both reCAPTCHA v2 (checkbox) and v3 (invisible / score-based).

## Scope

In scope:

- Add `IRecaptchaService` with a single `VerifyTokenAsync` method.
- Add `RecaptchaVerificationResult` model.
- Add `RecaptchaService` implementation that calls Google's siteverify API.
- Add reCAPTCHA configuration options to `DainnUserOptions`.
- Support v2 (checkbox) and v3 (score-based) verification.
- Configurable score threshold for v3.
- Unit and integration tests.

Out of scope:

- Applying reCAPTCHA to specific endpoints automatically; this library provides the service and consumers call it.
- Frontend widget rendering (PSA-92).
- Rate limiting integration (already handled by PSA-75 middleware).

## Existing Context

No existing reCAPTCHA code. Greenfield implementation.

Related existing features:
- `DainnUserOptions` with `EnableRateLimiting` and other security options.
- `AuthenticationService` with `RegisterAsync`, `LoginAsync`, `ForgotPasswordAsync`.
- Rate limiting middleware from PSA-75.

## Service Contract

Create `IRecaptchaService` in `DainnUser.Core.Interfaces.Services`:

```csharp
public interface IRecaptchaService
{
    Task<RecaptchaVerificationResult> VerifyTokenAsync(
        string token,
        string? action = null,
        CancellationToken cancellationToken = default);
}
```

## RecaptchaVerificationResult

Create in `DainnUser.Core.Models`:

```csharp
public class RecaptchaVerificationResult
{
    public bool Success { get; set; }
    public double Score { get; set; }
    public string? Hostname { get; set; }
    public string? Action { get; set; }
    public string[] ErrorCodes { get; set; } = Array.Empty<string>();
    public DateTime VerifiedAt { get; set; }
    public string? FailureReason { get; set; }
}
```

## RecaptchaService Implementation

`RecaptchaService` lives in `DainnUser.Application.Services`, depends on `HttpClient` and `IOptions<DainnUserOptions>`.

**VerifyTokenAsync flow:**
1. If `RecaptchaEnabled` is false: return `Success = true` (no-op).
2. POST to `https://www.google.com/recaptcha/api/siteverify` with:
   - `secret` (RecaptchaSecretKey)
   - `response` (token)
3. Parse JSON response.
4. For v3: check `score >= RecaptchaMinimumScore`.
5. For v2: check `success == true`.
6. Return `RecaptchaVerificationResult` with success, score, error codes.

**Google API response model:**

```json
{
  "success": true|false,
  "challenge_ts": "2024-01-01...",
  "hostname": "...",
  "score": 0.9,        // v3 only
  "action": "...",      // v3 only
  "error-codes": [...]  // optional
}
```

## Configuration

Add to `DainnUserOptions`:

```csharp
/// <summary>
/// Gets or sets a value indicating whether reCAPTCHA verification is enabled.
/// </summary>
public bool RecaptchaEnabled { get; set; } = false;

/// <summary>
/// Gets or sets the reCAPTCHA version ("v2" or "v3").
/// </summary>
public string RecaptchaVersion { get; set; } = "v3";

/// <summary>
/// Gets or sets the reCAPTCHA site key (public, used in frontend).
/// </summary>
public string RecaptchaSiteKey { get; set; } = string.Empty;

/// <summary>
/// Gets or sets the reCAPTCHA secret key (private, used for verification).
/// </summary>
public string RecaptchaSecretKey { get; set; } = string.Empty;

/// <summary>
/// Gets or sets the minimum score threshold for v3 verification.
/// Scores below this threshold are considered bot traffic.
/// </summary>
public double RecaptchaMinimumScore { get; set; } = 0.5;
```

## Error Handling

- Disabled reCAPTCHA: return `Success = true` (skip verification).
- Invalid token: return `Success = false` with error codes from Google.
- Network failure to Google API: return `Success = false` with `FailureReason = "reCAPTCHA verification service unavailable."`.
- Empty token: return `Success = false` with `FailureReason = "reCAPTCHA token is required."`.

## Where to Apply

Consumers call `IRecaptchaService.VerifyTokenAsync()` before these endpoints:

- Registration: verify before `RegisterAsync`.
- Login: verify before `LoginAsync`.
- Forgot password: verify before `ForgotPasswordAsync`.

The decision to reject or allow is made by the controller, not the service.

## Testing

Unit tests should cover:

1. Returns success when reCAPTCHA is disabled.
2. v3 returns success when score >= threshold.
3. v3 returns failure when score < threshold.
4. v2 returns success when Google says success.
5. v2 returns failure when Google says failure.
6. Returns failure when token is empty.
7. Returns failure when Google API is unreachable.

Using mocked `HttpClient` via `HttpMessageHandler`.

Integration tests:

1. Full v3 verification flow with mocked Google API responses.
2. Full v2 verification flow.
3. Disabled reCAPTCHA bypass.

## Security and Compatibility

- No breaking changes to existing service contracts.
- Secret key stored only server-side; never exposed to clients.
- No in-memory state.
- `IRecaptchaService` is injectable and overridable via DI.
