using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace DainnUser.UnitTests.Infrastructure;

public class EmailServiceTests
{
    private readonly Mock<IEmailProvider> _providerMock = new();
    private readonly Mock<ILogger<EmailService>> _loggerMock = new();
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        _service = new EmailService(_providerMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendEmailAsync_DelegatesToProvider()
    {
        _providerMock.Setup(p => p.SendEmailAsync(
            "test@example.com",
            "Test User",
            "Test Subject",
            "<p>Body</p>",
            null,
            default))
            .Returns(Task.CompletedTask);

        await _service.SendEmailAsync("test@example.com", "Test User", "Test Subject", "<p>Body</p>");

        _providerMock.Verify(p => p.SendEmailAsync(
            "test@example.com",
            "Test User",
            "Test Subject",
            "<p>Body</p>",
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_PassesAttachments()
    {
        var attachments = new List<EmailAttachment>
        {
            new() { FileName = "report.pdf", ContentType = "application/pdf", Content = new byte[] { 1, 2, 3 } }
        };

        _providerMock.Setup(p => p.SendEmailAsync(
            "test@example.com",
            null,
            "Subject",
            "<p>Body</p>",
            It.IsAny<IEnumerable<EmailAttachment>>(),
            default))
            .Returns(Task.CompletedTask);

        await _service.SendEmailAsync("test@example.com", null, "Subject", "<p>Body</p>", attachments);

        _providerMock.Verify(p => p.SendEmailAsync(
            "test@example.com",
            null,
            "Subject",
            "<p>Body</p>",
            attachments,
            default), Times.Once);
    }

    [Fact]
    public async Task SendEmailAsync_ThrowsAndLogsOnProviderFailure()
    {
        _providerMock.Setup(p => p.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<IEnumerable<EmailAttachment>?>(), default))
            .ThrowsAsync(new InvalidOperationException("SMTP connection failed"));

        var act = () => _service.SendEmailAsync("test@example.com", null, "Subject", "<p>Body</p>");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("SMTP connection failed");
    }

    [Fact]
    public async Task SendEmailVerificationAsync_CallsProviderWithCorrectSubject()
    {
        _providerMock.Setup(p => p.SendEmailAsync(
            "user@example.com",
            null,
            "Verify Your Email Address",
            It.Is<string>(b => b.Contains("verification-token")),
            null,
            default))
            .Returns(Task.CompletedTask);

        await _service.SendEmailVerificationAsync("user@example.com", "username", "verification-token");

        _providerMock.Verify(p => p.SendEmailAsync(
            "user@example.com",
            null,
            "Verify Your Email Address",
            It.Is<string>(b => b.Contains("username") && b.Contains("verification-token")),
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task SendPasswordResetAsync_CallsProviderWithCorrectSubject()
    {
        _providerMock.Setup(p => p.SendEmailAsync(
            "user@example.com",
            null,
            "Reset Your Password",
            It.IsAny<string>(),
            null,
            default))
            .Returns(Task.CompletedTask);

        await _service.SendPasswordResetAsync("user@example.com", "username", "reset-token");

        _providerMock.Verify(p => p.SendEmailAsync(
            "user@example.com",
            null,
            "Reset Your Password",
            It.Is<string>(b => b.Contains("reset-token")),
            null,
            default), Times.Once);
    }

    [Fact]
    public async Task SendTwoFactorCodeAsync_CallsProviderWithCorrectSubject()
    {
        _providerMock.Setup(p => p.SendEmailAsync(
            "user@example.com",
            null,
            "Your Two-Factor Authentication Code",
            It.Is<string>(b => b.Contains("123456")),
            null,
            default))
            .Returns(Task.CompletedTask);

        await _service.SendTwoFactorCodeAsync("user@example.com", "username", "123456");

        _providerMock.Verify(p => p.SendEmailAsync(
            "user@example.com",
            null,
            "Your Two-Factor Authentication Code",
            It.Is<string>(b => b.Contains("123456") && b.Contains("username")),
            null,
            default), Times.Once);
    }
}
