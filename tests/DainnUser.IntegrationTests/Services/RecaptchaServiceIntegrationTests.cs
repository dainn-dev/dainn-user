using System.Net;
using System.Text;
using System.Text.Json;
using DainnUser.Application.Services;
using DainnUser.Infrastructure.Configuration;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DainnUser.IntegrationTests.Services;

public class RecaptchaServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public RecaptchaServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task VerifyTokenAsync_V3TokenWithHighScore_PassesVerification()
    {
        // Arrange
        var options = new DainnUserOptions
        {
            RecaptchaEnabled = true,
            RecaptchaVersion = "v3",
            RecaptchaSecretKey = "test-secret",
            RecaptchaMinimumScore = 0.5
        };

        var mockHandler = new MockRecaptchaHttpMessageHandler(
            success: true,
            score: 0.9,
            action: "login",
            hostname: "example.com");

        var httpClient = new HttpClient(mockHandler);
        var service = new RecaptchaService(httpClient, Options.Create(options));

        // Act
        var result = await service.VerifyTokenAsync("valid-token-high-score");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().Be(0.9);
        result.Action.Should().Be("login");
        result.Hostname.Should().Be("example.com");
        result.FailureReason.Should().BeNullOrEmpty();
        result.ErrorCodes.Should().BeEmpty();
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task VerifyTokenAsync_V3TokenWithLowScore_FailsVerification()
    {
        // Arrange
        var options = new DainnUserOptions
        {
            RecaptchaEnabled = true,
            RecaptchaVersion = "v3",
            RecaptchaSecretKey = "test-secret",
            RecaptchaMinimumScore = 0.5
        };

        var mockHandler = new MockRecaptchaHttpMessageHandler(
            success: true,
            score: 0.1,
            action: "login",
            hostname: "example.com");

        var httpClient = new HttpClient(mockHandler);
        var service = new RecaptchaService(httpClient, Options.Create(options));

        // Act
        var result = await service.VerifyTokenAsync("valid-token-low-score");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Score.Should().Be(0.1);
        result.Action.Should().Be("login");
        result.Hostname.Should().Be("example.com");
        result.FailureReason.Should().Contain("Score 0.1 is below threshold 0.5");
        result.ErrorCodes.Should().BeEmpty();
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task VerifyTokenAsync_DisabledRecaptcha_ReturnsSuccessRegardless()
    {
        // Arrange
        var options = new DainnUserOptions
        {
            RecaptchaEnabled = false,
            RecaptchaVersion = "v3",
            RecaptchaSecretKey = "test-secret",
            RecaptchaMinimumScore = 0.5
        };

        // No need for mock handler since it should not make HTTP calls
        var httpClient = new HttpClient();
        var service = new RecaptchaService(httpClient, Options.Create(options));

        // Act
        var result = await service.VerifyTokenAsync("any-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().Be(1.0);
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    /// <summary>
    /// Mock HTTP message handler for testing reCAPTCHA verification.
    /// </summary>
    private class MockRecaptchaHttpMessageHandler : HttpMessageHandler
    {
        private readonly bool _success;
        private readonly double _score;
        private readonly string? _action;
        private readonly string? _hostname;
        private readonly string[]? _errorCodes;

        public MockRecaptchaHttpMessageHandler(
            bool success,
            double score = 0.0,
            string? action = null,
            string? hostname = null,
            string[]? errorCodes = null)
        {
            _success = success;
            _score = score;
            _action = action;
            _hostname = hostname;
            _errorCodes = errorCodes;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Mock Google reCAPTCHA siteverify endpoint
            if (request.RequestUri?.AbsolutePath.Contains("/siteverify") == true)
            {
                var response = new
                {
                    success = _success,
                    score = _score,
                    action = _action,
                    challenge_ts = DateTime.UtcNow.ToString("o"),
                    hostname = _hostname,
                    error_codes = _errorCodes
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(response),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
