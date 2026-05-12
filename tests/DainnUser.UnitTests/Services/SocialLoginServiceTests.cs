using System.Net;
using System.Text;
using System.Text.Json;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace DainnUser.UnitTests.Services;

public class SocialLoginServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly DainnUserOptions _options;
    private readonly SocialLoginService _socialLoginService;

    public SocialLoginServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _options = new DainnUserOptions
        {
            EnableSocialLogin = true,
            GoogleClientId = "test-client-id",
            GoogleClientSecret = "test-client-secret",
            FacebookAppId = "test-fb-app-id",
            FacebookAppSecret = "test-fb-secret",
            GitHubClientId = "test-github-client-id",
            GitHubClientSecret = "test-github-client-secret",
            MicrosoftClientId = "test-microsoft-client-id",
            MicrosoftClientSecret = "test-microsoft-client-secret",
            RefreshTokenExpirationDays = 7
        };

        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);

        _socialLoginService = new SocialLoginService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtTokenServiceMock.Object,
            _sessionServiceMock.Object,
            _passwordHasherMock.Object,
            Options.Create(_options),
            _httpClient);
    }

    private void SetupGoogleOAuthSuccess(string googleUserId = "google-123", string email = "user@example.com", string name = "Test User")
    {
        // Mock token exchange
        var tokenResponse = new
        {
            access_token = "google-access-token",
            refresh_token = "google-refresh-token",
            token_type = "Bearer"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://oauth2.googleapis.com/token"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            });

        // Mock user info fetch
        var userInfoResponse = new
        {
            sub = googleUserId,
            email = email,
            name = name,
            picture = "https://example.com/photo.jpg"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString() == "https://www.googleapis.com/oauth2/v3/userinfo"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(userInfoResponse), Encoding.UTF8, "application/json")
            });
    }

    private void SetupJwtTokenGeneration()
    {
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-token");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("refresh-token")).Returns("refresh-token-hash");

        // Mock session service
        _sessionServiceMock
            .Setup(x => x.CreateSessionAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SessionToken = "refresh-token-hash",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
    }

    private User CreateActiveUser(string email = "user@example.com", string username = "testuser")
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            EmailVerified = true,
            Status = UserStatus.Active,
            PasswordHash = "hashed",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task LoginWithGoogleAsync_AutoRegistersNewUser_WhenNoExistingMatch()
    {
        // Arrange
        SetupGoogleOAuthSuccess("google-new-user", "newuser@example.com", "New User");
        SetupJwtTokenGeneration();

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Google, "google-new-user", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("newuser@example.com", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.Users.AddLoginAsync(It.IsAny<UserLogin>(), default))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("random-password-hash");

        // Act
        var result = await _socialLoginService.LoginWithGoogleAsync(
            "auth-code", "https://example.com/callback", "127.0.0.1", "user-agent");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Email.Should().Be("newuser@example.com");
        result.User.EmailVerified.Should().BeTrue();

        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u =>
            u.Email == "newuser@example.com" &&
            u.Username == "newuser" &&
            u.EmailVerified == true &&
            u.Status == UserStatus.Active &&
            !string.IsNullOrEmpty(u.PasswordHash)
        ), default), Times.Once);

        _unitOfWorkMock.Verify(x => x.Users.AddLoginAsync(It.Is<UserLogin>(l =>
            l.Provider == LoginProvider.Google &&
            l.ProviderKey == "google-new-user" &&
            l.ProviderDisplayName == "New User"
        ), default), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_SignsInExistingUser_ByProviderKeyMatch()
    {
        // Arrange
        var existingUser = CreateActiveUser("existing@example.com", "existinguser");
        existingUser.Logins.Add(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = existingUser.Id,
            Provider = LoginProvider.Google,
            ProviderKey = "google-existing",
            ProviderDisplayName = "Existing User"
        });

        SetupGoogleOAuthSuccess("google-existing", "existing@example.com", "Existing User");
        SetupJwtTokenGeneration();

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Google, "google-existing", default))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(existingUser.Id, default))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _socialLoginService.LoginWithGoogleAsync(
            "auth-code", "https://example.com/callback", "127.0.0.1", "user-agent");

        // Assert
        result.Should().NotBeNull();
        result.User.Id.Should().Be(existingUser.Id);
        result.User.Email.Should().Be("existing@example.com");

        // Should NOT create a new user or new login
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Never);
        _unitOfWorkMock.Verify(x => x.Users.AddLoginAsync(It.IsAny<UserLogin>(), default), Times.Never);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_LinksGoogleAccount_WhenEmailMatchesExistingUser()
    {
        // Arrange
        var existingUser = CreateActiveUser("match@example.com", "matchuser");

        SetupGoogleOAuthSuccess("google-link", "match@example.com", "Match User");
        SetupJwtTokenGeneration();

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Google, "google-link", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("match@example.com", default))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(existingUser.Id, default))
            .ReturnsAsync(existingUser);
        _unitOfWorkMock.Setup(x => x.Users.AddLoginAsync(It.IsAny<UserLogin>(), default))
            .Returns(Task.CompletedTask);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _socialLoginService.LoginWithGoogleAsync(
            "auth-code", "https://example.com/callback", "127.0.0.1", "user-agent");

        // Assert
        result.Should().NotBeNull();
        result.User.Id.Should().Be(existingUser.Id);
        result.User.Email.Should().Be("match@example.com");

        // Should link Google account to existing user
        _unitOfWorkMock.Verify(x => x.Users.AddLoginAsync(It.Is<UserLogin>(l =>
            l.UserId == existingUser.Id &&
            l.Provider == LoginProvider.Google &&
            l.ProviderKey == "google-link"
        ), default), Times.Once);

        // Should NOT create a new user
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task LoginWithGoogleAsync_ThrowsInvalidOperationException_WhenSocialLoginDisabled()
    {
        // Arrange
        _options.EnableSocialLogin = false;

        // Act
        var act = async () => await _socialLoginService.LoginWithGoogleAsync(
            "auth-code", "https://example.com/callback", null, null);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Social login is not enabled.");

        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Never(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task LoginWithGoogleAsync_ThrowsInvalidOperationException_WhenAuthorizationCodeIsInvalid()
    {
        // Arrange
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString() == "https://oauth2.googleapis.com/token"),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\":\"invalid_grant\"}", Encoding.UTF8, "application/json")
            });

        // Act
        var act = async () => await _socialLoginService.LoginWithGoogleAsync(
            "invalid-code", "https://example.com/callback", null, null);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid Google authorization code.");
    }

    [Fact]
    public async Task LinkGoogleAccountAsync_CreatesUserLogin_ForAuthenticatedUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateActiveUser("link@example.com", "linkuser");
        user.Id = userId;

        SetupGoogleOAuthSuccess("google-link-new", "link@example.com", "Link User");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Google, "google-link-new", default))
            .ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Users.AddLoginAsync(It.IsAny<UserLogin>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _socialLoginService.LinkGoogleAccountAsync(
            userId, "auth-code", "https://example.com/callback");

        // Assert
        _unitOfWorkMock.Verify(x => x.Users.AddLoginAsync(It.Is<UserLogin>(l =>
            l.UserId == userId &&
            l.Provider == LoginProvider.Google &&
            l.ProviderKey == "google-link-new" &&
            l.ProviderDisplayName == "Link User"
        ), default), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LinkGoogleAccountAsync_ThrowsInvalidOperationException_WhenGoogleAccountAlreadyLinkedToAnotherUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateActiveUser("user1@example.com", "user1");
        user.Id = userId;

        var otherUserId = Guid.NewGuid();
        var otherUser = CreateActiveUser("user2@example.com", "user2");
        otherUser.Id = otherUserId;

        SetupGoogleOAuthSuccess("google-taken", "user1@example.com", "User 1");

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Google, "google-taken", default))
            .ReturnsAsync(otherUser);

        // Act
        var act = async () => await _socialLoginService.LinkGoogleAccountAsync(
            userId, "auth-code", "https://example.com/callback");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Google account is already linked to another user.");

        _unitOfWorkMock.Verify(x => x.Users.AddLoginAsync(It.IsAny<UserLogin>(), default), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UnlinkProviderAsync_RemovesUserLogin()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateActiveUser("unlink@example.com", "unlinkuser");
        user.Id = userId;
        user.PasswordHash = "has-password";

        var googleLogin = new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Google,
            ProviderKey = "google-unlink",
            ProviderDisplayName = "Unlink User"
        };
        user.Logins.Add(googleLogin);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _socialLoginService.UnlinkProviderAsync(userId, LoginProvider.Google);

        // Assert
        user.Logins.Should().NotContain(googleLogin);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UnlinkProviderAsync_ThrowsInvalidOperationException_WhenUnlinkingLastLoginMethod()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateActiveUser("lastmethod@example.com", "lastmethod");
        user.Id = userId;
        user.PasswordHash = string.Empty; // No password

        var googleLogin = new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Google,
            ProviderKey = "google-only",
            ProviderDisplayName = "Only Login"
        };
        user.Logins.Add(googleLogin);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _socialLoginService.UnlinkProviderAsync(userId, LoginProvider.Google);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot unlink the only login method.");

        user.Logins.Should().Contain(googleLogin);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UnlinkProviderAsync_AllowsUnlinking_WhenUserHasPassword()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateActiveUser("haspassword@example.com", "haspassword");
        user.Id = userId;
        user.PasswordHash = "valid-password-hash";

        var googleLogin = new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Google,
            ProviderKey = "google-with-password",
            ProviderDisplayName = "Google User"
        };
        user.Logins.Add(googleLogin);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _socialLoginService.UnlinkProviderAsync(userId, LoginProvider.Google);

        // Assert
        user.Logins.Should().NotContain(googleLogin);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UnlinkProviderAsync_AllowsUnlinking_WhenUserHasOtherProviders()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = CreateActiveUser("multiprovider@example.com", "multiprovider");
        user.Id = userId;
        user.PasswordHash = string.Empty; // No password

        var googleLogin = new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Google,
            ProviderKey = "google-multi",
            ProviderDisplayName = "Google"
        };
        var facebookLogin = new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Facebook,
            ProviderKey = "facebook-multi",
            ProviderDisplayName = "Facebook"
        };
        user.Logins.Add(googleLogin);
        user.Logins.Add(facebookLogin);

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _socialLoginService.UnlinkProviderAsync(userId, LoginProvider.Google);

        // Assert
        user.Logins.Should().NotContain(googleLogin);
        user.Logins.Should().Contain(facebookLogin);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LoginWithFacebookAsync_NewUser_AutoRegistersAndLogsIn()
    {
        SetupFacebookOAuthSuccess();

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Facebook, "fb-123", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("fb@example.com", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .Returns((User u, CancellationToken _) => Task.FromResult(u));
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(CreateTestUser());
        SetupJwtMocks();

        var result = await _socialLoginService.LoginWithFacebookAsync("fb-auth-code", "http://localhost/callback", "127.0.0.1", "TestAgent");

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("fb@example.com");
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginWithFacebookAsync_ExistingFacebookUser_LogsIn()
    {
        SetupFacebookOAuthSuccess();

        var existingUser = CreateTestUser();
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Facebook, "fb-123", default))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(existingUser.Id, default))
            .ReturnsAsync(existingUser);
        SetupJwtMocks();

        var result = await _socialLoginService.LoginWithFacebookAsync("fb-auth-code", "http://localhost/callback", "127.0.0.1", "TestAgent");

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(existingUser.Email);
    }

    [Fact]
    public async Task LoginWithFacebookAsync_ExistingEmailUser_LinksAccountAndLogsIn()
    {
        SetupFacebookOAuthSuccess();

        var existingUser = CreateTestUser();
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Facebook, "fb-123", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("fb@example.com", default))
            .ReturnsAsync(existingUser);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(existingUser.Id, default))
            .ReturnsAsync(existingUser);
        SetupJwtMocks();

        var result = await _socialLoginService.LoginWithFacebookAsync("fb-auth-code", "http://localhost/callback", "127.0.0.1", "TestAgent");

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeast(1));
    }

    [Fact]
    public async Task LoginWithFacebookAsync_SocialLoginDisabled_ThrowsException()
    {
        var disabledOptions = new DainnUserOptions
        {
            EnableSocialLogin = false,
            FacebookAppId = "test",
            FacebookAppSecret = "test"
        };
        var service = new SocialLoginService(
            _userRepositoryMock.Object, _unitOfWorkMock.Object,
            _jwtTokenServiceMock.Object, _sessionServiceMock.Object,
            _passwordHasherMock.Object, Options.Create(disabledOptions), _httpClient);

        await service.Invoking(s => s.LoginWithFacebookAsync("code", "callback", null, null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not enabled*");
    }

    [Fact]
    public async Task LinkFacebookAccountAsync_NewFacebookAccount_LinksSuccessfully()
    {
        SetupFacebookOAuthSuccess();

        var user = CreateTestUser();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Facebook, "fb-123", default))
            .ReturnsAsync((User?)null);

        await _socialLoginService.LinkFacebookAccountAsync(user.Id, "fb-auth-code", "http://localhost/callback");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LinkFacebookAccountAsync_AlreadyLinkedToSameUser_DoesNothing()
    {
        SetupFacebookOAuthSuccess();

        var user = CreateTestUser();
        user.Logins.Add(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = LoginProvider.Facebook,
            ProviderKey = "fb-123"
        });

        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Facebook, "fb-123", default))
            .ReturnsAsync(user);

        await _socialLoginService.LinkFacebookAccountAsync(user.Id, "fb-auth-code", "http://localhost/callback");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task LinkFacebookAccountAsync_AlreadyLinkedToOtherUser_ThrowsException()
    {
        SetupFacebookOAuthSuccess();

        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        user2.Email = "other@example.com";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(user1.Id, default))
            .ReturnsAsync(user1);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Facebook, "fb-123", default))
            .ReturnsAsync(user2);

        await _socialLoginService.Invoking(s => s.LinkFacebookAccountAsync(user1.Id, "fb-auth-code", "http://localhost/callback"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already linked*");
    }

    [Fact]
    public async Task LoginWithGitHubAsync_NewUser_AutoRegistersAndLogsIn()
    {
        SetupGitHubOAuthSuccess();
        SetupJwtMocks();

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.GitHub, "gh-123", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("gh@example.com", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .Returns((User u, CancellationToken _) => Task.FromResult(u));
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken _) => CreateTestUser());
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed-password");

        var result = await _socialLoginService.LoginWithGitHubAsync("gh-auth-code", "http://localhost/callback", null, null);

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("gh@example.com");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeast(1));
    }

    [Fact]
    public async Task LoginWithGitHubAsync_ExistingGitHubUser_LogsIn()
    {
        SetupGitHubOAuthSuccess();
        SetupJwtMocks();

        var user = CreateTestUser();
        user.Logins.Add(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = LoginProvider.GitHub,
            ProviderKey = "gh-123"
        });

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.GitHub, "gh-123", default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default))
            .ReturnsAsync(user);

        var result = await _socialLoginService.LoginWithGitHubAsync("gh-auth-code", "http://localhost/callback", null, null);

        result.Should().NotBeNull();
        result.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task LoginWithGitHubAsync_ExistingEmailUser_LinksAccountAndLogsIn()
    {
        SetupGitHubOAuthSuccess();
        SetupJwtMocks();

        var user = CreateTestUser();
        user.Email = "gh@example.com";

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.GitHub, "gh-123", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("gh@example.com", default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default))
            .ReturnsAsync(user);

        var result = await _socialLoginService.LoginWithGitHubAsync("gh-auth-code", "http://localhost/callback", null, null);

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeast(1));
    }

    [Fact]
    public async Task LoginWithGitHubAsync_SocialLoginDisabled_ThrowsException()
    {
        var disabledOptions = new DainnUserOptions
        {
            EnableSocialLogin = false,
            GitHubClientId = "test",
            GitHubClientSecret = "test"
        };
        var service = new SocialLoginService(
            _userRepositoryMock.Object, _unitOfWorkMock.Object,
            _jwtTokenServiceMock.Object, _sessionServiceMock.Object,
            _passwordHasherMock.Object, Options.Create(disabledOptions), _httpClient);

        await service.Invoking(s => s.LoginWithGitHubAsync("code", "callback", null, null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not enabled*");
    }

    [Fact]
    public async Task LinkGitHubAccountAsync_NewGitHubAccount_LinksSuccessfully()
    {
        SetupGitHubOAuthSuccess();

        var user = CreateTestUser();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.GitHub, "gh-123", default))
            .ReturnsAsync((User?)null);

        await _socialLoginService.LinkGitHubAccountAsync(user.Id, "gh-auth-code", "http://localhost/callback");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LinkGitHubAccountAsync_AlreadyLinkedToSameUser_DoesNothing()
    {
        SetupGitHubOAuthSuccess();

        var user = CreateTestUser();
        user.Logins.Add(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = LoginProvider.GitHub,
            ProviderKey = "gh-123"
        });

        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.GitHub, "gh-123", default))
            .ReturnsAsync(user);

        await _socialLoginService.LinkGitHubAccountAsync(user.Id, "gh-auth-code", "http://localhost/callback");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task LinkGitHubAccountAsync_AlreadyLinkedToOtherUser_ThrowsException()
    {
        SetupGitHubOAuthSuccess();

        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        user2.Email = "other@example.com";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(user1.Id, default))
            .ReturnsAsync(user1);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.GitHub, "gh-123", default))
            .ReturnsAsync(user2);

        await _socialLoginService.Invoking(s => s.LinkGitHubAccountAsync(user1.Id, "gh-auth-code", "http://localhost/callback"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already linked*");
    }

    [Fact]
    public async Task LoginWithMicrosoftAsync_NewUser_AutoRegistersAndLogsIn()
    {
        SetupMicrosoftOAuthSuccess();
        SetupJwtMocks();

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Microsoft, "ms-123", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("ms@example.com", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(It.IsAny<string>(), null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .Returns((User u, CancellationToken _) => Task.FromResult(u));
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken _) => CreateTestUser());
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed-password");

        var result = await _socialLoginService.LoginWithMicrosoftAsync("ms-auth-code", "http://localhost/callback", null, null);

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("ms@example.com");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeast(1));
    }

    [Fact]
    public async Task LoginWithMicrosoftAsync_ExistingMicrosoftUser_LogsIn()
    {
        SetupMicrosoftOAuthSuccess();
        SetupJwtMocks();

        var user = CreateTestUser();
        user.Logins.Add(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = LoginProvider.Microsoft,
            ProviderKey = "ms-123"
        });

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Microsoft, "ms-123", default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default))
            .ReturnsAsync(user);

        var result = await _socialLoginService.LoginWithMicrosoftAsync("ms-auth-code", "http://localhost/callback", null, null);

        result.Should().NotBeNull();
        result.User.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task LoginWithMicrosoftAsync_ExistingEmailUser_LinksAccountAndLogsIn()
    {
        SetupMicrosoftOAuthSuccess();
        SetupJwtMocks();

        var user = CreateTestUser();
        user.Email = "ms@example.com";

        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Microsoft, "ms-123", default))
            .ReturnsAsync((User?)null);
        _userRepositoryMock.Setup(x => x.GetByEmailAsync("ms@example.com", default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default))
            .ReturnsAsync(user);

        var result = await _socialLoginService.LoginWithMicrosoftAsync("ms-auth-code", "http://localhost/callback", null, null);

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.AtLeast(1));
    }

    [Fact]
    public async Task LoginWithMicrosoftAsync_SocialLoginDisabled_ThrowsException()
    {
        var disabledOptions = new DainnUserOptions
        {
            EnableSocialLogin = false,
            MicrosoftClientId = "test",
            MicrosoftClientSecret = "test"
        };
        var service = new SocialLoginService(
            _userRepositoryMock.Object, _unitOfWorkMock.Object,
            _jwtTokenServiceMock.Object, _sessionServiceMock.Object,
            _passwordHasherMock.Object, Options.Create(disabledOptions), _httpClient);

        await service.Invoking(s => s.LoginWithMicrosoftAsync("code", "callback", null, null))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not enabled*");
    }

    [Fact]
    public async Task LinkMicrosoftAccountAsync_NewMicrosoftAccount_LinksSuccessfully()
    {
        SetupMicrosoftOAuthSuccess();

        var user = CreateTestUser();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Microsoft, "ms-123", default))
            .ReturnsAsync((User?)null);

        await _socialLoginService.LinkMicrosoftAccountAsync(user.Id, "ms-auth-code", "http://localhost/callback");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LinkMicrosoftAccountAsync_AlreadyLinkedToSameUser_DoesNothing()
    {
        SetupMicrosoftOAuthSuccess();

        var user = CreateTestUser();
        user.Logins.Add(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = LoginProvider.Microsoft,
            ProviderKey = "ms-123"
        });

        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Microsoft, "ms-123", default))
            .ReturnsAsync(user);

        await _socialLoginService.LinkMicrosoftAccountAsync(user.Id, "ms-auth-code", "http://localhost/callback");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task LinkMicrosoftAccountAsync_AlreadyLinkedToOtherUser_ThrowsException()
    {
        SetupMicrosoftOAuthSuccess();

        var user1 = CreateTestUser();
        var user2 = CreateTestUser();
        user2.Email = "other@example.com";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(user1.Id, default))
            .ReturnsAsync(user1);
        _userRepositoryMock.Setup(x => x.GetByExternalLoginAsync(LoginProvider.Microsoft, "ms-123", default))
            .ReturnsAsync(user2);

        await _socialLoginService.Invoking(s => s.LinkMicrosoftAccountAsync(user1.Id, "ms-auth-code", "http://localhost/callback"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already linked*");
    }

    private void SetupMicrosoftOAuthSuccess(string msUserId = "ms-123", string email = "ms@example.com", string displayName = "Microsoft User")
    {
        _options.MicrosoftClientId = "test-microsoft-client-id";
        _options.MicrosoftClientSecret = "test-microsoft-client-secret";

        var tokenResponse = new
        {
            access_token = "ms-access-token",
            token_type = "Bearer",
            expires_in = 3600,
            scope = "openid profile email"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().StartsWith("https://login.microsoftonline.com/common/oauth2/v2.0/token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().StartsWith("https://graph.microsoft.com/v1.0/me")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = msUserId,
                    displayName = displayName,
                    mail = email,
                    userPrincipalName = email
                }), Encoding.UTF8, "application/json")
            });
    }

    private void SetupGitHubOAuthSuccess(string ghUserId = "gh-123", string email = "gh@example.com", string login = "ghuser", string name = "GitHub User")
    {
        _options.GitHubClientId = "test-github-client-id";
        _options.GitHubClientSecret = "test-github-client-secret";

        var tokenResponse = new
        {
            access_token = "gh-access-token",
            token_type = "bearer",
            scope = "user:email"
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().StartsWith("https://github.com/login/oauth/access_token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().StartsWith("https://api.github.com/user") &&
                    !req.RequestUri.ToString().Contains("/emails")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = ghUserId,
                    login = login,
                    name = name,
                    email = email,
                    avatar_url = "https://example.com/gh.jpg"
                }), Encoding.UTF8, "application/json")
            });
    }

    private void SetupFacebookOAuthSuccess(string fbUserId = "fb-123", string email = "fb@example.com", string name = "Facebook User")
    {
        _options.FacebookAppId = "test-fb-app-id";
        _options.FacebookAppSecret = "test-fb-secret";

        var tokenResponse = new
        {
            access_token = "fb-access-token",
            token_type = "Bearer",
            expires_in = 5184000
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().StartsWith("https://graph.facebook.com/v18.0/oauth/access_token")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(tokenResponse), Encoding.UTF8, "application/json")
            });

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().StartsWith("https://graph.facebook.com/v18.0/me")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    id = fbUserId,
                    email = email,
                    name = name,
                    picture = new { data = new { url = "https://example.com/fb.jpg" } }
                }), Encoding.UTF8, "application/json")
            });
    }

    private void SetupJwtMocks()
    {
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<List<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("access-token", DateTime.UtcNow.AddHours(1)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken(It.IsAny<string>()))
            .Returns("hashed-refresh");
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Returns(Task.CompletedTask);
        _sessionServiceMock.Setup(x => x.CreateSessionAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ReturnsAsync(new UserSession
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                SessionToken = "hashed-refresh",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
    }

    private User CreateTestUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Logins = new List<UserLogin>(),
            UserRoles = new List<UserRole>()
        };
        return user;
    }
}
