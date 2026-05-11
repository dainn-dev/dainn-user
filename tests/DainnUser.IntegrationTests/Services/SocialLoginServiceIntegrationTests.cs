using System.Net;
using System.Text;
using System.Text.Json;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Repositories;
using DainnUser.Infrastructure.Services;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DainnUser.IntegrationTests.Services;

public class SocialLoginServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly SocialLoginService _socialLoginService;
    private readonly UserRepository _userRepository;
    private readonly UnitOfWork _unitOfWork;
    private readonly JwtTokenService _jwtTokenService;
    private readonly SessionService _sessionService;
    private readonly DainnUserOptions _options;
    private readonly HttpClient _httpClient;

    public SocialLoginServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase();

        _userRepository = new UserRepository(_fixture.DbContext);
        _unitOfWork = new UnitOfWork(_fixture.DbContext);

        _options = new DainnUserOptions
        {
            EnableSocialLogin = true,
            GoogleClientId = "test-client-id",
            GoogleClientSecret = "test-client-secret",
            JwtExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7,
            EnableSessionManagement = true,
            MaxActiveSessionsPerUser = 5
        };

        var jwtOptions = new JwtOptions
        {
            Secret = "test-secret-key-minimum-32-characters-long-for-security",
            Issuer = "test-issuer",
            Audience = "test-audience"
        };

        _jwtTokenService = new JwtTokenService(Options.Create(jwtOptions), _options);

        var sessionRepository = new SessionRepository(_fixture.DbContext);
        _sessionService = new SessionService(
            sessionRepository,
            _userRepository,
            _unitOfWork,
            Options.Create(_options));

        // Create HttpClient with mock handler
        var mockHandler = new MockHttpMessageHandler();
        _httpClient = new HttpClient(mockHandler);

        _socialLoginService = new SocialLoginService(
            _userRepository,
            _unitOfWork,
            _jwtTokenService,
            _sessionService,
            new PasswordHasher<User>(),
            Options.Create(_options),
            _httpClient);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_NewUser_AutoRegistersAndReturnsLoginResult()
    {
        // Arrange
        var authCode = "test-auth-code";
        var callbackUrl = "https://example.com/callback";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0 Test";

        // Act
        var result = await _socialLoginService.LoginWithGoogleAsync(
            authCode, callbackUrl, ipAddress, userAgent);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("testuser@gmail.com");
        result.User.Username.Should().StartWith("testuser");
        result.User.EmailVerified.Should().BeTrue();
        result.RequiresTwoFactor.Should().BeFalse();

        // Verify user was created in database
        var userInDb = await _fixture.DbContext.Users
            .Include(u => u.Logins)
            .FirstOrDefaultAsync(u => u.Email == "testuser@gmail.com");

        userInDb.Should().NotBeNull();
        userInDb!.Status.Should().Be(UserStatus.Active);
        userInDb.EmailVerified.Should().BeTrue();
        userInDb.Logins.Should().ContainSingle();
        userInDb.Logins!.First().Provider.Should().Be(LoginProvider.Google);
        userInDb.Logins.First().ProviderKey.Should().Be("google-user-123");
        userInDb.Logins.First().ProviderDisplayName.Should().Be("Test User");

        // Verify session was created
        var session = await _fixture.DbContext.UserSessions
            .FirstOrDefaultAsync(s => s.UserId == userInDb.Id);
        session.Should().NotBeNull();
        session!.IsActive.Should().BeTrue();
        session.IpAddress.Should().Be(ipAddress);
        session.UserAgent.Should().Be(userAgent);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_ExistingUserByEmail_LinksGoogleAccountAndReturnsLoginResult()
    {
        // Arrange
        var existingUser = await CreateUserAsync("testuser@gmail.com", "existinguser");
        var authCode = "test-auth-code";
        var callbackUrl = "https://example.com/callback";

        // Act
        var result = await _socialLoginService.LoginWithGoogleAsync(
            authCode, callbackUrl, null, null);

        // Assert
        result.Should().NotBeNull();
        result.User.Id.Should().Be(existingUser.Id);
        result.User.Email.Should().Be(existingUser.Email);
        result.User.Username.Should().Be(existingUser.Username);

        // Verify Google login was linked
        var userInDb = await _fixture.DbContext.Users
            .Include(u => u.Logins)
            .FirstAsync(u => u.Id == existingUser.Id);

        userInDb.Logins.Should().ContainSingle();
        userInDb.Logins!.First().Provider.Should().Be(LoginProvider.Google);
        userInDb.Logins.First().ProviderKey.Should().Be("google-user-123");
    }

    [Fact]
    public async Task LinkGoogleAccountAsync_ValidUser_LinksGoogleAccount()
    {
        // Arrange
        var user = await CreateUserAsync("linktest@example.com", "linkuser");
        var authCode = "test-auth-code";
        var callbackUrl = "https://example.com/callback";

        // Act
        await _socialLoginService.LinkGoogleAccountAsync(
            user.Id, authCode, callbackUrl);

        // Assert
        var userInDb = await _fixture.DbContext.Users
            .Include(u => u.Logins)
            .FirstAsync(u => u.Id == user.Id);

        userInDb.Logins.Should().ContainSingle();
        userInDb.Logins!.First().Provider.Should().Be(LoginProvider.Google);
        userInDb.Logins.First().ProviderKey.Should().Be("google-user-123");
        userInDb.Logins.First().ProviderDisplayName.Should().Be("Test User");
        userInDb.Logins.First().LinkedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UnlinkProviderAsync_ExistingGoogleLogin_RemovesLogin()
    {
        // Arrange
        var user = await CreateUserAsync("unlink@example.com", "unlinkuser");

        // Add Google login
        var googleLogin = new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = LoginProvider.Google,
            ProviderKey = "google-user-456",
            ProviderDisplayName = "Unlink Test User",
            LinkedAt = DateTime.UtcNow
        };
        _fixture.DbContext.UserLogins.Add(googleLogin);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify login exists
        var loginsBefore = await _fixture.DbContext.UserLogins
            .Where(l => l.UserId == user.Id)
            .ToListAsync();
        loginsBefore.Should().ContainSingle();

        // Act
        await _socialLoginService.UnlinkProviderAsync(user.Id, LoginProvider.Google);

        // Assert
        var loginsAfter = await _fixture.DbContext.UserLogins
            .Where(l => l.UserId == user.Id)
            .ToListAsync();
        loginsAfter.Should().BeEmpty();
    }

    private async Task<User> CreateUserAsync(string email, string username)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            Status = UserStatus.Active,
            EmailVerified = true,
            PasswordHash = "dummy-hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();
        return user;
    }

    /// <summary>
    /// Mock HTTP message handler for testing Google OAuth flow.
    /// </summary>
    private class MockHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Mock token exchange endpoint
            if (request.RequestUri?.AbsolutePath.Contains("/token") == true)
            {
                var tokenResponse = new
                {
                    access_token = "mock-access-token",
                    refresh_token = "mock-refresh-token",
                    token_type = "Bearer"
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(tokenResponse),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            // Mock user info endpoint
            if (request.RequestUri?.AbsolutePath.Contains("/userinfo") == true)
            {
                var userInfo = new
                {
                    sub = "google-user-123",
                    email = "testuser@gmail.com",
                    name = "Test User",
                    picture = "https://example.com/photo.jpg"
                };

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        JsonSerializer.Serialize(userInfo),
                        Encoding.UTF8,
                        "application/json")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }
    }
}
