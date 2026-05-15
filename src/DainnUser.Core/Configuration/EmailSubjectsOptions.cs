namespace DainnUser.Core.Configuration;

/// <summary>
/// Configurable subject lines for emails sent by DainnUser. Each property is bound from
/// <c>DainnUser:Email:Subjects:*</c>; defaults preserve the prior hard-coded strings so existing
/// consumers see no behaviour change. Use this to differentiate sender/tenant branding without
/// forking the library or replacing <see cref="DainnUser.Core.Interfaces.Services.IEmailService"/>.
/// </summary>
public class EmailSubjectsOptions
{
    /// <summary>
    /// Subject for the email-verification message sent on registration and resend.
    /// </summary>
    public string EmailVerification { get; set; } = "Verify Your Email Address";

    /// <summary>
    /// Subject for the password-reset message.
    /// </summary>
    public string PasswordReset { get; set; } = "Reset Your Password";

    /// <summary>
    /// Subject for the post-reset confirmation message that no token is included in.
    /// </summary>
    public string PasswordChanged { get; set; } = "Your Password Has Been Changed";

    /// <summary>
    /// Subject for the account-lockout notification sent on the lock-triggering attempt.
    /// </summary>
    public string AccountLockout { get; set; } = "Account Locked - Security Alert";

    /// <summary>
    /// Subject for the two-factor authentication code message.
    /// </summary>
    public string TwoFactorCode { get; set; } = "Your Two-Factor Authentication Code";

    /// <summary>
    /// Subject for the generic contact-verification code message sent by
    /// <c>EmailContactVerificationSender</c>.
    /// </summary>
    public string ContactVerification { get; set; } = "Verify Contact";
}
