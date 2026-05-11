using System.Net;
using System.Text;
using System.Text.Json;
using DainnUser.Application.Services;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace DainnUser.UnitTests.Services;

public class RecaptchaServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly DainnUserOptions _options;
    private readonly RecaptchaService _recaptchaService;

    public RecaptchaServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _options = new DainnUserOptions
        {
            RecaptchaEnabled = true,
            RecaptchaVersion = "v3",
            RecaptchaSecretKey = "test-secret",
            RecaptchaMinimumScore = 0.5
        };

        _recaptchaService = new RecaptchaService(_httpClient, Options.Create(_options));
    }

    private void SetupGoogleRecaptchaResponse(bool success, double score = 0.0, string? action = null, string? hostname = null, string[]? errorCodes = null)
    {
        var response = new Dictionary<string, object>
        {
            ["success"] = success,
            ["challenge_ts"] = "2026-05-11T08:00:00Z",
            ["hostname"] = hostname ?? "test"
        };

        if (success) response["score"] = score;
        if (action != null) response["action"] = action;
        if (errorCodes != null) response["error-codes"] = errorCodes;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://www.google.com/recaptcha/api/siteverify"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(response), Encoding.UTF8, "application/json")
            });
    }

    [Fact]
    public async Task VerifyTokenAsync_ReturnsSuccess_WhenRecaptchaIsDisabled()
    {
        // Arrange
        _options.RecaptchaEnabled = false;

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("any-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().Be(1.0);
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Should not call Google API
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task VerifyTokenAsync_V3_ReturnsSuccess_WhenScoreIsAboveThreshold()
    {
        // Arrange
        _options.RecaptchaVersion = "v3";
        _options.RecaptchaMinimumScore = 0.5;
        SetupGoogleRecaptchaResponse(success: true, score: 0.9, action: "login", hostname: "test.com");

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("valid-token", "login");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().Be(0.9);
        result.Action.Should().Be("login");
        result.Hostname.Should().Be("test.com");
        result.FailureReason.Should().BeNull();
        result.ErrorCodes.Should().BeEmpty();
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task VerifyTokenAsync_V3_ReturnsFailure_WhenScoreIsBelowThreshold()
    {
        // Arrange
        _options.RecaptchaVersion = "v3";
        _options.RecaptchaMinimumScore = 0.5;
        SetupGoogleRecaptchaResponse(success: true, score: 0.3, action: "login", hostname: "test.com");

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("low-score-token", "login");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Score.Should().Be(0.3);
        result.Action.Should().Be("login");
        result.Hostname.Should().Be("test.com");
        result.FailureReason.Should().Be("Score 0.3 is below threshold 0.5.");
        result.ErrorCodes.Should().BeEmpty();
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task VerifyTokenAsync_V2_ReturnsSuccess_WhenGoogleSaysSuccess()
    {
        // Arrange
        _options.RecaptchaVersion = "v2";
        SetupGoogleRecaptchaResponse(success: true, hostname: "test.com");

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("valid-v2-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().Be(1.0);
        result.Hostname.Should().Be("test.com");
        result.FailureReason.Should().BeNull();
        result.ErrorCodes.Should().BeEmpty();
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task VerifyTokenAsync_V2_ReturnsFailure_WhenGoogleSaysFailure()
    {
        // Arrange
        _options.RecaptchaVersion = "v2";
        SetupGoogleRecaptchaResponse(success: false, hostname: "test.com", errorCodes: new[] { "invalid-input-response", "timeout-or-duplicate" });

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("invalid-v2-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Score.Should().Be(0.0);
        result.Hostname.Should().Be("test.com");
        result.FailureReason.Should().Be("invalid-input-response, timeout-or-duplicate");
        result.ErrorCodes.Should().BeEquivalentTo(new[] { "invalid-input-response", "timeout-or-duplicate" });
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task VerifyTokenAsync_ReturnsFailure_WhenTokenIsEmptyOrWhitespace(string? token)
    {
        // Act
        var result = await _recaptchaService.VerifyTokenAsync(token!);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("reCAPTCHA token is required.");
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Should not call Google API
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task VerifyTokenAsync_ReturnsFailure_WhenHttpRequestFails()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://www.google.com/recaptcha/api/siteverify"),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("valid-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("reCAPTCHA verification service is unavailable.");
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task VerifyTokenAsync_HonorsDefaultV3Behavior()
    {
        // Arrange - using default options (v3, threshold 0.5)
        var defaultOptions = new DainnUserOptions
        {
            RecaptchaEnabled = true,
            RecaptchaVersion = "v3",
            RecaptchaSecretKey = "test-secret",
            RecaptchaMinimumScore = 0.5
        };
        var service = new RecaptchaService(_httpClient, Options.Create(defaultOptions));
        SetupGoogleRecaptchaResponse(success: true, score: 0.7, action: "submit", hostname: "test.com");

        // Act
        var result = await service.VerifyTokenAsync("token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().Be(0.7);
        result.Action.Should().Be("submit");
        result.Hostname.Should().Be("test.com");
    }

    [Fact]
    public async Task VerifyTokenAsync_ReturnsFailure_WhenRequestTimesOut()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://www.google.com/recaptcha/api/siteverify"),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timed out"));

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("valid-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("reCAPTCHA verification timed out.");
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task VerifyTokenAsync_ReturnsFailure_WhenGoogleReturnsNullResponse()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://www.google.com/recaptcha/api/siteverify"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("null", Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("valid-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.FailureReason.Should().Be("reCAPTCHA verification service returned an invalid response.");
        result.VerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task VerifyTokenAsync_V3_HandlesExactThresholdScore()
    {
        // Arrange
        _options.RecaptchaVersion = "v3";
        _options.RecaptchaMinimumScore = 0.5;
        SetupGoogleRecaptchaResponse(success: true, score: 0.5, action: "login", hostname: "test.com");

        // Act
        var result = await _recaptchaService.VerifyTokenAsync("exact-threshold-token");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Score.Should().Be(0.5);
        result.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task VerifyTokenAsync_SendsCorrectRequestToGoogle()
    {
        // Arrange
        SetupGoogleRecaptchaResponse(success: true, score: 0.9);
        HttpRequestMessage? capturedRequest = null;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    success = true,
                    score = 0.9,
                    action = "login",
                    hostname = "test.com"
                }), Encoding.UTF8, "application/json")
            });

        // Act
        await _recaptchaService.VerifyTokenAsync("test-token");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedRequest.RequestUri.Should().Be("https://www.google.com/recaptcha/api/siteverify");

        var content = await capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("secret=test-secret");
        content.Should().Contain("response=test-token");
    }
}
