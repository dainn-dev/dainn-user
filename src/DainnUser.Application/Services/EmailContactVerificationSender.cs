using DainnUser.Core.Configuration;
using DainnUser.Core.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

/// <summary>
/// Sends email contact verification codes through the configured email service.
/// </summary>
public class EmailContactVerificationSender : IContactVerificationSender
{
    private readonly IEmailService _emailService;
    private readonly EmailSubjectsOptions _subjects;

    public EmailContactVerificationSender(IEmailService emailService, IOptions<EmailSubjectsOptions> subjects)
    {
        _emailService = emailService;
        _subjects = subjects.Value;
    }

    public string ContactType => "Email";

    public async Task SendVerificationCodeAsync(string contactValue, string code, CancellationToken cancellationToken = default)
    {
        var body = $"""
            <!DOCTYPE html>
            <html>
            <body>
                <p>Your contact verification code is:</p>
                <p style="font-size:24px;font-weight:bold;letter-spacing:4px">{code}</p>
                <p>This code expires in 10 minutes.</p>
            </body>
            </html>
            """;

        await _emailService.SendEmailAsync(contactValue, null, _subjects.ContactVerification, body, null, cancellationToken);
    }
}
