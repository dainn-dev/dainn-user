using System.Text.Json;
using System.Text.Json.Serialization;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Recaptcha;
using DainnUser.Core.Configuration;
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
