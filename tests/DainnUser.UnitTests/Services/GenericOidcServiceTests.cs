using System.Security.Claims;
using DainnUser.Application.Services;
using DainnUser.Core.Configuration;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Moq;

namespace DainnUser.UnitTests.Services;

public class GenericOidcServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
    private readonly DainnUserOptions _options;
    private readonly GenericOidcService _service;

    public GenericOidcServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _sessionServiceMock = new Mock<ISessionService>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();

        _options = new DainnUserOptions
        {
            EnableGenericOidc = true,
            EnableSessionManagement = false,
            RefreshTokenExpirationDays = 7,
            OidcProviders = new List<OidcProviderConfig>
            {
                new OidcProviderConfig
                {
                    ProviderId = "auth0",
                    DisplayName = "Auth0",
                    Authority = "https://test.auth0.com",
                    ClientId = "test-client-id",
                    ClientSecret = "test-client-secret",
                    CallbackPath = "/signin-oidc",
                    Scope = "openid profile email",
                    EmailClaimType = "email",
                    NameClaimType = "name",
                    SubjectClaimType = "sub"
                }
            }
        };

        _unitOfWorkMock.SetupGet(u => u.Users).Returns(_userRepositoryMock.Object);

        _service = new GenericOidcService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _jwtTokenServiceMock.Object,
            _sessionServiceMock.Object,
            _passwordHasherMock.Object,
            Options.Create(_options));
    }

    private ClaimsPrincipal CreateOidcClaimsPrincipal(
        string email = "user@example.com",
        string name = "Test User",
        string subject = "auth0|123456",
        string? emailVerified = "true")
    {
        var claims = new List<Claim>
        {
            new Claim("email", email),
            new Claim("name", name),
            new Claim("sub", subject)
        };

        if (emailVerified is not null)
            claims.Add(new Claim("email_verified", emailVerified));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));
    }

    private void SetupJwtMocks()
    {
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(60)));

        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("access-token", DateTime.UtcNow.AddMinutes(60)));

        _jwtTokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("refresh-token");

        _jwtTokenServiceMock
            .Setup(x => x.HashRefreshToken("refresh-token"))
            .Returns("refresh-token-hash");
    }

    [Fact]
    public async Task LoginWithOidcAsync_ThrowsException_WhenGenericOidcDisabled()
    {
        // Arrange
        _options.EnableGenericOidc = false;
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        // Act
        var act = async () => await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Generic OIDC authentication is not enabled");
    }

    [Fact]
    public async Task LoginWithOidcAsync_ThrowsException_WhenProviderNotConfigured()
    {
        // Arrange
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        // Act
        var act = async () => await _service.LoginWithOidcAsync(
            "unknown-provider", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OIDC provider 'unknown-provider' is not configured");
    }

    [Fact]
    public async Task LoginWithOidcAsync_ThrowsException_WhenEmailClaimMissing()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("name", "Test User"),
            new Claim("sub", "auth0|123456")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));

        // Act
        var act = async () => await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email claim not found in OIDC response");
    }

    [Fact]
    public async Task LoginWithOidcAsync_ThrowsException_WhenSubjectClaimMissing()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("email", "user@example.com"),
            new Claim("name", "Test User")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));

        // Act
        var act = async () => await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subject claim not found in OIDC response");
    }

    [Fact]
    public async Task LoginWithOidcAsync_ReturnsLoginResult_WhenUserExistsByProviderKey()
    {
        // Arrange
        var claimsPrincipal = CreateOidcClaimsPrincipal();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            EmailVerified = true,
            Status = UserStatus.Active,
            UserRoles = new List<UserRole>()
        };

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                LoginProvider.GenericOidc,
                "oidc:auth0:auth0%7C123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userRepositoryMock
            .Setup(x => x.GetWithRolesAsync(existingUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        SetupJwtMocks();

        // Act
        var result = await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("refresh-token");
        result.User.Should().NotBeNull();
        result.User.Email.Should().Be("user@example.com");
    }

    [Fact]
    public async Task LoginWithOidcAsync_LinksAccountAndReturnsLoginResult_WhenUserExistsByEmail()
    {
        // Arrange
        var claimsPrincipal = CreateOidcClaimsPrincipal();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            EmailVerified = true,
            Status = UserStatus.Active,
            UserRoles = new List<UserRole>()
        };

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                LoginProvider.GenericOidc,
                "oidc:auth0:auth0%7C123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userRepositoryMock
            .Setup(x => x.GetWithRolesAsync(existingUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        SetupJwtMocks();

        // Act
        var result = await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.AddLoginAsync(
            It.Is<UserLogin>(ul =>
                ul.UserId == existingUser.Id &&
                ul.Provider == LoginProvider.GenericOidc &&
                ul.ProviderKey == "oidc:auth0:auth0%7C123456" &&
                ul.ProviderDisplayName == "Test User"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LoginWithOidcAsync_AutoRegistersNewUser_WhenUserDoesNotExist()
    {
        // Arrange
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                It.IsAny<LoginProvider>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, CancellationToken ct) =>
            {
                var user = new User
                {
                    Id = userId,
                    Email = "user@example.com",
                    Username = "user",
                    EmailVerified = true,
                    Status = UserStatus.Active,
                    UserRoles = new List<UserRole>()
                };
                return user;
            });

        _passwordHasherMock
            .Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed-password");

        SetupJwtMocks();

        // Act
        var result = await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.AddAsync(
            It.Is<User>(u =>
                u.Email == "user@example.com" &&
                u.EmailVerified == true &&
                u.Status == UserStatus.Active),
            It.IsAny<CancellationToken>()), Times.Once);
        _userRepositoryMock.Verify(x => x.AddLoginAsync(
            It.Is<UserLogin>(ul =>
                ul.Provider == LoginProvider.GenericOidc &&
                ul.ProviderKey == "oidc:auth0:auth0%7C123456"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task LinkOidcAccountAsync_ThrowsException_WhenGenericOidcDisabled()
    {
        // Arrange
        _options.EnableGenericOidc = false;
        var userId = Guid.NewGuid();
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        // Act
        var act = async () => await _service.LinkOidcAccountAsync(
            userId, "auth0", claimsPrincipal);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Generic OIDC authentication is not enabled");
    }

    [Fact]
    public async Task LinkOidcAccountAsync_ThrowsException_WhenSubjectClaimMissing()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new Claim("email", "user@example.com"),
            new Claim("name", "Test User")
        };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));

        // Act
        var act = async () => await _service.LinkOidcAccountAsync(
            userId, "auth0", claimsPrincipal);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Subject claim not found in OIDC response");
    }

    [Fact]
    public async Task LinkOidcAccountAsync_LinksAccount_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                LoginProvider.GenericOidc,
                "oidc:auth0:auth0%7C123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        await _service.LinkOidcAccountAsync(userId, "auth0", claimsPrincipal);

        // Assert
        _userRepositoryMock.Verify(x => x.AddLoginAsync(
            It.Is<UserLogin>(ul =>
                ul.UserId == userId &&
                ul.Provider == LoginProvider.GenericOidc &&
                ul.ProviderKey == "oidc:auth0:auth0%7C123456"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LinkOidcAccountAsync_ThrowsException_WhenProviderAlreadyLinkedToAnotherUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        var otherUser = new User
        {
            Id = otherUserId,
            Email = "other@example.com",
            Username = "otheruser"
        };

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                LoginProvider.GenericOidc,
                "oidc:auth0:auth0%7C123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherUser);

        // Act
        var act = async () => await _service.LinkOidcAccountAsync(
            userId, "auth0", claimsPrincipal);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("OIDC provider 'auth0' is already linked to another account");
    }

    [Fact]
    public void GetConfiguredProviders_ReturnsConfiguredProviders()
    {
        // Act
        var providers = _service.GetConfiguredProviders();

        // Assert
        providers.Should().NotBeNull();
        providers.Should().HaveCount(1);
        providers.First().ProviderId.Should().Be("auth0");
    }

    [Fact]
    public void GetConfiguredProviders_ReturnsEmpty_WhenNoProvidersConfigured()
    {
        // Arrange
        _options.OidcProviders = new List<OidcProviderConfig>();

        // Act
        var providers = _service.GetConfiguredProviders();

        // Assert
        providers.Should().NotBeNull();
        providers.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LoginWithOidcAsync_ThrowsArgumentException_WhenProviderIdNullOrEmpty(string? providerId)
    {
        // Arrange
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        // Act
        var act = async () => await _service.LoginWithOidcAsync(
            providerId!, claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("providerId");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task LinkOidcAccountAsync_ThrowsArgumentException_WhenProviderIdNullOrEmpty(string? providerId)
    {
        // Arrange
        var claimsPrincipal = CreateOidcClaimsPrincipal();

        // Act
        var act = async () => await _service.LinkOidcAccountAsync(
            Guid.NewGuid(), providerId!, claimsPrincipal);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("providerId");
    }

    [Fact]
    public async Task LoginWithOidcAsync_ThrowsException_WhenAutoLinkingAndEmailNotVerified()
    {
        // Arrange — email_verified claim is "false"
        var claimsPrincipal = CreateOidcClaimsPrincipal(emailVerified: "false");
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            EmailVerified = true,
            Status = UserStatus.Active,
            UserRoles = new List<UserRole>()
        };

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                LoginProvider.GenericOidc,
                "oidc:auth0:auth0%7C123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var act = async () => await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*did not confirm email verification*");
    }

    [Fact]
    public async Task LoginWithOidcAsync_ThrowsException_WhenAutoLinkingAndEmailVerifiedClaimMissing()
    {
        // Arrange — email_verified claim is absent
        var claimsPrincipal = CreateOidcClaimsPrincipal(emailVerified: null);
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            EmailVerified = true,
            Status = UserStatus.Active,
            UserRoles = new List<UserRole>()
        };

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                LoginProvider.GenericOidc,
                "oidc:auth0:auth0%7C123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        // Act
        var act = async () => await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*did not confirm email verification*");
    }

    [Fact]
    public async Task LoginWithOidcAsync_AutoLinksAccount_WhenRequireEmailVerifiedDisabledAndClaimMissing()
    {
        // Arrange — provider has RequireEmailVerifiedForAutoLink = false
        _options.OidcProviders[0].RequireEmailVerifiedForAutoLink = false;
        var claimsPrincipal = CreateOidcClaimsPrincipal(emailVerified: null);
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "testuser",
            EmailVerified = true,
            Status = UserStatus.Active,
            UserRoles = new List<UserRole>()
        };

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                LoginProvider.GenericOidc,
                "oidc:auth0:auth0%7C123456",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userRepositoryMock
            .Setup(x => x.GetWithRolesAsync(existingUser.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        SetupJwtMocks();

        // Act
        var result = await _service.LoginWithOidcAsync(
            "auth0", claimsPrincipal, "127.0.0.1", "test-agent");

        // Assert
        result.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.AddLoginAsync(
            It.Is<UserLogin>(ul => ul.UserId == existingUser.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("john.doe@example.com", "johndoe")]
    [InlineData("user+tag@example.com", "usertag")]
    [InlineData("test-user@example.com", "testuser")]
    [InlineData("123@example.com", "123")]
    [InlineData("+++@example.com", "user")]
    public async Task LoginWithOidcAsync_SanitizesUsername_WhenAutoRegisteringNewUser(
        string email, string expectedBaseUsername)
    {
        // Arrange
        var claimsPrincipal = CreateOidcClaimsPrincipal(email: email);
        _options.OidcProviders[0].EmailClaimType = "email";

        _userRepositoryMock
            .Setup(x => x.GetByExternalLoginAsync(
                It.IsAny<LoginProvider>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        _userRepositoryMock
            .Setup(x => x.GetWithRolesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid userId, CancellationToken ct) => new User
            {
                Id = userId, Email = email, Username = expectedBaseUsername,
                EmailVerified = true, Status = UserStatus.Active, UserRoles = new List<UserRole>()
            });

        _passwordHasherMock
            .Setup(x => x.HashPassword(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed-password");

        SetupJwtMocks();

        // Act
        await _service.LoginWithOidcAsync("auth0", claimsPrincipal, null, null);

        // Assert — user was added with sanitized username
        _userRepositoryMock.Verify(x => x.AddAsync(
            It.Is<User>(u => u.Username == expectedBaseUsername),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
