using System.Net;
using System.Net.Http.Headers;
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
/// End-to-end integration tests for authentication flows using TestServer.
/// </summary>
public class AuthControllerEndToEndTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerEndToEndTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task EndToEnd_RegisterVerifyLoginUseTokenLogout_Success()
    {
        // Step 1: Register
        var registerRequest = new RegisterDto
        {
            Email = "e2e1@test.com",
            Username = "e2euser1",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        registerResult.Should().NotBeNull();
        registerResult!.Success.Should().BeTrue();
        var userId = registerResult.Data!.UserId;
        userId.Should().NotBeEmpty();

        // Step 2: Read the plain verification token captured by the mocked email service
        // (DB stores SHA-256 hash, which is unusable as the token input).
        var verificationToken = _factory.EmailTokens.GetVerification("e2e1@test.com");

        // Step 3: Verify email
        var verifyRequest = new VerifyEmailRequest
        {
            UserId = userId,
            Token = verificationToken
        };

        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        verifyResult.Should().NotBeNull();
        verifyResult!.Success.Should().BeTrue();

        // Step 4: Login
        var loginRequest = new LoginDto
        {
            Email = "e2e1@test.com",
            Password = "SecurePass123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data.Should().NotBeNull();
        loginResult.Data!.AccessToken.Should().NotBeNullOrEmpty();
        loginResult.Data.RefreshToken.Should().NotBeNullOrEmpty();
        var accessToken = loginResult.Data.AccessToken;
        var sessionId = loginResult.Data.SessionId;

        // Step 5: Use access token to access protected endpoint (logout)
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var logoutResult = await logoutResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        logoutResult.Should().NotBeNull();
        logoutResult!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task EndToEnd_RegisterLoginChangePasswordLoginWithNewPassword_Success()
    {
        // Step 1: Register
        var registerRequest = new RegisterDto
        {
            Email = "e2e2@test.com",
            Username = "e2euser2",
            Password = "OldPass123!",
            ConfirmPassword = "OldPass123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        var userId = registerResult!.Data!.UserId;

        // Step 2: Verify email using the plain token captured by the mocked email service.
        var verificationToken = _factory.EmailTokens.GetVerification("e2e2@test.com");

        var verifyRequest = new VerifyEmailRequest
        {
            UserId = userId,
            Token = verificationToken
        };
        await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Step 3: Login
        var loginRequest = new LoginDto
        {
            Email = "e2e2@test.com",
            Password = "OldPass123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var accessToken = loginResult!.Data!.AccessToken;

        // Step 4: Change password
        var changePasswordRequest = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!",
            NewPassword = "NewPass123!",
            ConfirmNewPassword = "NewPass123!"
        };

        var client2 = _factory.CreateClient();
        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var changePasswordResponse = await client2.PostAsJsonAsync("/api/auth/change-password", changePasswordRequest);
        changePasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var changePasswordResult = await changePasswordResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        changePasswordResult.Should().NotBeNull();
        changePasswordResult!.Success.Should().BeTrue();

        // Step 5: Login with new password
        var loginRequest2 = new LoginDto
        {
            Email = "e2e2@test.com",
            Password = "NewPass123!"
        };

        var loginResponse2 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest2);
        loginResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult2 = await loginResponse2.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        loginResult2.Should().NotBeNull();
        loginResult2!.Success.Should().BeTrue();
        loginResult2.Data!.AccessToken.Should().NotBeNullOrEmpty();

        // Step 6: Verify old password no longer works
        var loginRequest3 = new LoginDto
        {
            Email = "e2e2@test.com",
            Password = "OldPass123!"
        };

        var loginResponse3 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest3);
        loginResponse3.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EndToEnd_ForgotPasswordResetPasswordLoginWithNewPassword_Success()
    {
        // Step 1: Register and verify
        var registerRequest = new RegisterDto
        {
            Email = "e2e3@test.com",
            Username = "e2euser3",
            Password = "OriginalPass123!",
            ConfirmPassword = "OriginalPass123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        var userId = registerResult!.Data!.UserId;

        // Verify email using the plain token captured by the mocked email service.
        var verificationToken = _factory.EmailTokens.GetVerification("e2e3@test.com");

        var verifyRequest = new VerifyEmailRequest
        {
            UserId = userId,
            Token = verificationToken
        };
        await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);

        // Step 2: Forgot password
        var forgotPasswordRequest = new ForgotPasswordDto
        {
            Email = "e2e3@test.com"
        };

        var forgotPasswordResponse = await _client.PostAsJsonAsync("/api/auth/forgot-password", forgotPasswordRequest);
        forgotPasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Read the plain reset token captured by the mocked email service.
        var resetToken = _factory.EmailTokens.GetPasswordReset("e2e3@test.com");

        // Step 4: Reset password
        var resetPasswordRequest = new ResetPasswordDto
        {
            Token = resetToken,
            NewPassword = "ResetPass123!",
            ConfirmPassword = "ResetPass123!"
        };

        var resetPasswordResponse = await _client.PostAsJsonAsync("/api/auth/reset-password", resetPasswordRequest);
        resetPasswordResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resetPasswordResult = await resetPasswordResponse.Content.ReadFromJsonAsync<ApiResponse<string>>();
        resetPasswordResult.Should().NotBeNull();
        resetPasswordResult!.Success.Should().BeTrue();

        // Step 5: Login with new password
        var loginRequest = new LoginDto
        {
            Email = "e2e3@test.com",
            Password = "ResetPass123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        loginResult.Should().NotBeNull();
        loginResult!.Success.Should().BeTrue();
        loginResult.Data!.AccessToken.Should().NotBeNullOrEmpty();

        // Step 6: Verify old password no longer works
        var loginRequest2 = new LoginDto
        {
            Email = "e2e3@test.com",
            Password = "OriginalPass123!"
        };

        var loginResponse2 = await _client.PostAsJsonAsync("/api/auth/login", loginRequest2);
        loginResponse2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
