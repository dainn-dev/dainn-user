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
using Xunit.Abstractions;

namespace DainnUser.IntegrationTests.Api;

public class DiagnosticTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private readonly ITestOutputHelper _output;

    public DiagnosticTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task Diagnostic_JwtTokenInfo()
    {
        // Register
        var registerRequest = new RegisterDto
        {
            Email = "diag@test.com",
            Username = "diagnostic",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        _output.WriteLine($"Register: {registerResponse.StatusCode}");
        var registerBody = await registerResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Register Body: {registerBody}");

        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<RegisterResponse>>();
        var userId = registerResult!.Data!.UserId;

        // Verify
        string verificationToken;
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<DainnUserDbContext>();
            var token = await dbContext.UserTokens
                .Where(t => t.UserId == userId && t.TokenType == TokenType.EmailVerification && !t.IsUsed)
                .FirstOrDefaultAsync();
            verificationToken = token!.TokenValue;
        }

        var verifyRequest = new VerifyEmailRequest { UserId = userId, Token = verificationToken };
        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyRequest);
        _output.WriteLine($"Verify: {verifyResponse.StatusCode}");

        // Login
        var loginRequest = new LoginDto { Email = "diag@test.com", Password = "SecurePass123!" };
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        _output.WriteLine($"Login: {loginResponse.StatusCode}");
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Login Body: {loginBody}");

        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        var accessToken = loginResult!.Data!.AccessToken;
        var sessionId = loginResult.Data.SessionId;
        _output.WriteLine($"SessionId: {sessionId}");
        _output.WriteLine($"Token (first 50 chars): {accessToken[..Math.Min(50, accessToken.Length)]}...");

        // Try logout
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var logoutResponse = await _client.PostAsync("/api/auth/logout", null);
        var logoutBody = await logoutResponse.Content.ReadAsStringAsync();
        _output.WriteLine($"Logout Status: {logoutResponse.StatusCode}");
        _output.WriteLine($"Logout Body: {logoutBody}");

        // Try to get WWW-Authenticate header
        if (logoutResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            var wwwAuth = logoutResponse.Headers.WwwAuthenticate.ToString();
            _output.WriteLine($"WWW-Authenticate: {wwwAuth}");
        }

        // Assert logout succeeded
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
