namespace DainnUser.Infrastructure.Configuration;

/// <summary>
/// Configuration options for DainnUser library.
/// </summary>
public class DainnUserOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether social login is enabled.
    /// </summary>
    public bool EnableSocialLogin { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether two-factor authentication is enabled.
    /// </summary>
    public bool EnableTwoFactor { get; set; } = false;

    /// <summary>
    /// Gets or sets how many minutes the pending 2FA setup token is valid.
    /// </summary>
    public int TwoFactorSetupExpirationMinutes { get; set; } = 10;

    /// <summary>
    /// Gets or sets how many days a remember-device token remains valid.
    /// </summary>
    public int TwoFactorRememberDeviceDays { get; set; } = 30;

    /// <summary>
    /// Gets or sets a value indicating whether email verification is required for registration.
    /// </summary>
    public bool RequireEmailVerification { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether phone verification is enabled.
    /// </summary>
    public bool EnablePhoneVerification { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether account lockout is enabled.
    /// </summary>
    public bool EnableAccountLockout { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum failed login attempts before lockout.
    /// </summary>
    public int MaxFailedLoginAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the lockout duration in minutes.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Gets or sets a value indicating whether session management is enabled.
    /// </summary>
    public bool EnableSessionManagement { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether activity logging is enabled.
    /// </summary>
    public bool EnableActivityLogging { get; set; } = true;

    /// <summary>
    /// Gets or sets the JWT token expiration in minutes.
    /// </summary>
    public int JwtExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Gets or sets the refresh token expiration in days.
    /// </summary>
    public int RefreshTokenExpirationDays { get; set; } = 7;

    /// <summary>
    /// Gets or sets the password reset token expiration in hours. Tokens are one-time-use and
    /// invalidated on first reset; this value bounds the time-of-use window.
    /// </summary>
    public int PasswordResetTokenExpirationHours { get; set; } = 1;

    /// <summary>
    /// Gets or sets the maximum number of active sessions allowed per user.
    /// When exceeded, the oldest session is deactivated.
    /// </summary>
    public int MaxActiveSessionsPerUser { get; set; } = 5;

    /// <summary>
    /// Gets or sets the login history retention period in days.
    /// Records older than this will be eligible for cleanup.
    /// </summary>
    public int LoginHistoryRetentionDays { get; set; } = 90;

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

    /// <summary>
    /// Gets or sets the Facebook OAuth app ID.
    /// </summary>
    public string FacebookAppId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Facebook OAuth app secret.
    /// </summary>
    public string FacebookAppSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Facebook OAuth callback path.
    /// </summary>
    public string FacebookCallbackPath { get; set; } = "/signin-facebook";

    /// <summary>
    /// Gets or sets the Microsoft OAuth client ID.
    /// </summary>
    public string MicrosoftClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Microsoft OAuth client secret.
    /// </summary>
    public string MicrosoftClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Microsoft OAuth callback path.
    /// </summary>
    public string MicrosoftCallbackPath { get; set; } = "/signin-microsoft";

    /// <summary>
    /// Gets or sets the GitHub OAuth client ID.
    /// </summary>
    public string GitHubClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub OAuth client secret.
    /// </summary>
    public string GitHubClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the GitHub OAuth callback path.
    /// </summary>
    public string GitHubCallbackPath { get; set; } = "/signin-github";

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

    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Gets or sets the rate limit requests per minute.
    /// </summary>
    public int RateLimitRequestsPerMinute { get; set; } = 60;
}
