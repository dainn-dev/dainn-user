# PSA-74 Google reCAPTCHA Integration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `IRecaptchaService` / `RecaptchaService` with v2 and v3 support, config via `DainnUserOptions`, and configurable score threshold for bot protection.

**Architecture:** RecaptchaService POSTs to Google's siteverify API via HttpClient. Returns `RecaptchaVerificationResult` with success/failure/score/error codes. Consumer controllers decide whether to reject based on result.

**Tech Stack:** .NET 8, Moq, FluentAssertions

---

## File Map

```
src/DainnUser.Core/Models/Recaptcha/
  + RecaptchaVerificationResult.cs   ← new

src/DainnUser.Core/Interfaces/Services/
  + IRecaptchaService.cs             ← new

src/DainnUser.Application/Services/
  + RecaptchaService.cs              ← new

src/DainnUser.Application/
  ApplicationServiceExtensions.cs    ← modify (add DI registration)

src/DainnUser.Infrastructure/Configuration/
  DainnUserOptions.cs               ← modify (add reCAPTCHA options)

tests/DainnUser.UnitTests/Services/
  + RecaptchaServiceTests.cs        ← new

tests/DainnUser.IntegrationTests/Services/
  + RecaptchaServiceIntegrationTests.cs ← new
```

---

## Task 1: Add reCAPTCHA options to DainnUserOptions

**Files:**
- Modify: `src/DainnUser.Infrastructure/Configuration/DainnUserOptions.cs`

Add after `GoogleCallbackPath` property:

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

---

## Task 2: Create RecaptchaVerificationResult model

**Files:**
- Create: `src/DainnUser.Core/Models/Recaptcha/RecaptchaVerificationResult.cs`

```csharp
namespace DainnUser.Core.Models.Recaptcha;

/// <summary>
/// Result of a reCAPTCHA token verification.
/// </summary>
public class RecaptchaVerificationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the verification succeeded.
    /// For v3, this is true when score >= threshold.
    /// For v2, this matches Google's success field.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the score from reCAPTCHA v3 (0.0 to 1.0).
    /// Always 1.0 for v2 success responses.
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// Gets or sets the hostname that the reCAPTCHA was solved on.
    /// </summary>
    public string? Hostname { get; set; }

    /// <summary>
    /// Gets or sets the action name for reCAPTCHA v3.
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Gets or sets the error codes returned by Google.
    /// </summary>
    public string[] ErrorCodes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the UTC timestamp when verification was performed.
    /// </summary>
    public DateTime VerifiedAt { get; set; }

    /// <summary>
    /// Gets or sets a human-readable failure reason for non-Google errors
    /// (e.g., network failure, empty token).
    /// </summary>
    public string? FailureReason { get; set; }
}
```

---

## Task 3: Create IRecaptchaService interface

**Files:**
- Create: `src/DainnUser.Core/Interfaces/Services/IRecaptchaService.cs`

```csharp
using DainnUser.Core.Models.Recaptcha;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service interface for Google reCAPTCHA verification.
/// </summary>
public interface IRecaptchaService
{
    /// <summary>
    /// Verifies a reCAPTCHA token with Google's siteverify API.
    /// </summary>
    /// <param name="token">The reCAPTCHA response token from the frontend.</param>
    /// <param name="action">The action name for v3 verification (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A verification result indicating success or failure.</returns>
    Task<RecaptchaVerificationResult> VerifyTokenAsync(
        string token,
        string? action = null,
        CancellationToken cancellationToken = default);
}
```

---

## Task 4: Create RecaptchaService implementation

**Files:**
- Create: `src/DainnUser.Application/Services/RecaptchaService.cs`

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Recaptcha;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for Google reCAPTCHA verification.
/// </summary>
public class RecaptchaService : IRecaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly DainnUserOptions _options;

    private const string SiteVerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    /// <summary>
    /// Initializes a new instance of the <see cref="RecaptchaService"/> class.
    /// </summary>
    public RecaptchaService(HttpClient httpClient, IOptions<DainnUserOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<RecaptchaVerificationResult> VerifyTokenAsync(
        string token,
        string? action = null,
        CancellationToken ct = default)
    {
        if (!_options.RecaptchaEnabled)
        {
            return new RecaptchaVerificationResult
            {
                Success = true,
                Score = 1.0,
                VerifiedAt = DateTime.UtcNow
            };
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return new RecaptchaVerificationResult
            {
                Success = false,
                FailureReason = "reCAPTCHA token is required.",
                VerifiedAt = DateTime.UtcNow
            };
        }

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["secret"] = _options.RecaptchaSecretKey,
            ["response"] = token
        });

        try
        {
            var response = await _httpClient.PostAsync(SiteVerifyUrl, content, ct);
            var json = await response.Content.ReadAsStringAsync(ct);
            var googleResult = JsonSerializer.Deserialize<GoogleRecaptchaResponse>(json);

            if (googleResult is null)
            {
                return Fail("reCAPTCHA verification service returned an invalid response.");
            }

            var isV3 = string.Equals(_options.RecaptchaVersion, "v3", StringComparison.OrdinalIgnoreCase);

            if (isV3)
            {
                var scoreOk = googleResult.Score >= _options.RecaptchaMinimumScore;
                return new RecaptchaVerificationResult
                {
                    Success = googleResult.Success && scoreOk,
                    Score = googleResult.Score,
                    Hostname = googleResult.Hostname,
                    Action = googleResult.Action,
                    ErrorCodes = googleResult.ErrorCodes ?? Array.Empty<string>(),
                    VerifiedAt = DateTime.UtcNow,
                    FailureReason = !scoreOk ? $"Score {googleResult.Score} is below threshold {_options.RecaptchaMinimumScore}." : null
                };
            }

            // v2
            return new RecaptchaVerificationResult
            {
                Success = googleResult.Success,
                Score = googleResult.Success ? 1.0 : 0.0,
                Hostname = googleResult.Hostname,
                ErrorCodes = googleResult.ErrorCodes ?? Array.Empty<string>(),
                VerifiedAt = DateTime.UtcNow,
                FailureReason = !googleResult.Success ? string.Join(", ", googleResult.ErrorCodes ?? Array.Empty<string>()) : null
            };
        }
        catch (HttpRequestException)
        {
            return Fail("reCAPTCHA verification service is unavailable.");
        }
        catch (TaskCanceledException)
        {
            return Fail("reCAPTCHA verification timed out.");
        }
    }

    private static RecaptchaVerificationResult Fail(string reason)
    {
        return new RecaptchaVerificationResult
        {
            Success = false,
            FailureReason = reason,
            VerifiedAt = DateTime.UtcNow
        };
    }

    private class GoogleRecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
```

---

## Task 5: Register RecaptchaService in DI

**Files:**
- Modify: `src/DainnUser.Application/ApplicationServiceExtensions.cs`

Add alongside other service registrations:

```csharp
services.AddScoped<IRecaptchaService, RecaptchaService>();
```

---

## Task 6: Write unit tests for RecaptchaService

**Files:**
- Create: `tests/DainnUser.UnitTests/Services/RecaptchaServiceTests.cs`

Test cases (8):
1. Returns success when reCAPTCHA is disabled.
2. v3 returns success when score >= threshold.
3. v3 returns failure when score < threshold.
4. v2 returns success when Google says success.
5. v2 returns failure when Google says failure.
6. Returns failure when token is empty.
7. Returns failure when Google API returns HTTP error.
8. Default options produce correct v3 behavior.

Use Moq for `HttpClient` via `HttpMessageHandler`. Use FluentAssertions.

---

## Task 7: Write integration tests for RecaptchaService

**Files:**
- Create: `tests/DainnUser.IntegrationTests/Services/RecaptchaServiceIntegrationTests.cs`

Use existing `DatabaseFixture`.

Test cases (3):
1. v3 token with high score passes.
2. v3 token with low score fails.
3. Disabled reCAPTCHA returns success.

---

## Verification

After all tasks:
1. Run: `dotnet build` — must pass with no errors.
2. Run: `dotnet test` — all tests must pass.

---

## Spec Coverage Check

| Requirement | Task |
|---|---|
| `IRecaptchaService` interface | Task 3 |
| `VerifyRecaptchaAsync()` (VerifyTokenAsync) | Tasks 3, 4 |
| `RecaptchaVerificationResult` model | Task 2 |
| reCAPTCHA v2 support | Tasks 4, 6, 7 |
| reCAPTCHA v3 support | Tasks 4, 6, 7 |
| Enable/disable via configuration | Tasks 1, 4 |
| Score threshold for v3 | Tasks 1, 4, 6 |
| SiteKey / SecretKey config | Task 1 |
| Unit tests | Task 6 |
| Integration tests | Task 7 |
