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
