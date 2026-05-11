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
