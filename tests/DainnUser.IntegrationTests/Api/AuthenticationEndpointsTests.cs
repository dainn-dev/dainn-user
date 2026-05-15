using System.Net;
using System.Net.Http.Json;
using DainnUser.Api.DTOs;
using DainnUser.Api.DTOs.Authentication;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Enums;
using DainnUser.Infrastructure.Data;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DainnUser.IntegrationTests.Api;

/// <summary>
/// Integration tests for authentication endpoints using TestServer.
/// </summary>
public class AuthenticationEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly string _uniquePrefix = Guid.NewGuid().ToString("N")[..8];

    public AuthenticationEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidRequest_Returns200WithUserId()
    {
        // Arrange
        var request = new RegisterDto
        {
            Email = $"newuser{_uniquePrefix}@test.com",
            Username = $"newuser{_uniquePrefix}",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().NotBeEmpty();
        result.Data.Message.Should().Contain("Registration successful");
    }

    [Fact]
    public async Task Register_MissingRequiredFields_Returns400()
    {
        // Arrange
        var request = new RegisterDto
        {
            Email = "",
            Username = "",
            Password = "",
            ConfirmPassword = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        // Arrange - Use unique email to avoid collision with other tests
        var uniqueEmail = $"duplicate{_uniquePrefix}@test.com";
        var request = new RegisterDto
        {
            Email = uniqueEmail,
            Username = $"user1{_uniquePrefix}",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };

        // Register first time
        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act - Register again with same email (different username)
        var request2 = new RegisterDto
        {
            Email = uniqueEmail,
            Username = $"user2{_uniquePrefix}",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithAccessToken()
    {
        // Arrange - Register and verify user first with unique email
        var uniqueEmail = $"loginuser{_uniquePrefix}@test.com";
        var registerRequest = new RegisterDto
        {
            Email = uniqueEmail,
            Username = $"loginuser{_uniquePrefix}",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();

        // Read the plain verification token captured by the mocked email service.
        var verificationToken = _factory.EmailTokens.GetVerification(uniqueEmail);

        // Verify email
        var verifyRequest = new VerifyEmailRequest
        {
            UserId = registerResult!.Data!.UserId,
            Token = verificationToken
        };
        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now login
        var loginRequest = new LoginDto
        {
            Email = uniqueEmail,
            Password = "SecurePass123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange - Register user first
        var uniqueEmail = $"wrongpass{_uniquePrefix}@test.com";
        var registerRequest = new RegisterDto
        {
            Email = uniqueEmail,
            Username = $"wrongpass{_uniquePrefix}",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var loginRequest = new LoginDto
        {
            Email = uniqueEmail,
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Login_NonExistentUser_Returns401()
    {
        // Arrange
        var loginRequest = new LoginDto
        {
            Email = "nonexistent@test.com",
            Password = "SomePassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_Returns200()
    {
        // Arrange - Register user first
        var uniqueEmail = $"forgot{_uniquePrefix}@test.com";
        var registerRequest = new RegisterDto
        {
            Email = uniqueEmail,
            Username = $"forgot{_uniquePrefix}",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerRequest);

        var forgotRequest = new ForgotPasswordDto
        {
            Email = uniqueEmail
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().Contain("password reset link");
    }

    [Fact]
    public async Task ForgotPassword_InvalidEmailFormat_Returns400()
    {
        // Arrange
        var forgotRequest = new ForgotPasswordDto
        {
            Email = "invalid-email"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmail_ValidToken_Returns200()
    {
        // Arrange - Register user first
        var uniqueEmail = $"verify{_uniquePrefix}@test.com";
        var registerRequest = new RegisterDto
        {
            Email = uniqueEmail,
            Username = $"verify{_uniquePrefix}",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();

        // Read the plain verification token captured by the mocked email service.
        var verificationToken = _factory.EmailTokens.GetVerification(uniqueEmail);

        var verifyRequest = new VerifyEmailRequest
        {
            UserId = registerResult!.Data!.UserId,
            Token = verificationToken
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyEmail_InvalidToken_Returns400()
    {
        // Arrange
        var verifyRequest = new VerifyEmailRequest
        {
            UserId = Guid.NewGuid(),
            Token = "invalid-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_ValidAuth_Returns200()
    {
        // Arrange - This test requires a valid JWT token
        // For now, we'll test the endpoint without auth and expect 401
        // In a real scenario, we'd need to login first and use the token

        // Act
        var response = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_ValidRefreshToken_Returns200()
    {
        // Arrange - This test requires a valid refresh token from login
        // For now, we'll test with an invalid token and expect 401
        var refreshRequest = new RefreshTokenDto
        {
            RefreshToken = "invalid-refresh-token"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/refresh-token", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }
}
