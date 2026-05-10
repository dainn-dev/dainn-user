using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace DainnUser.UnitTests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILoginHistoryRepository> _loginHistoryRepositoryMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly DainnUserOptions _options;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loginHistoryRepositoryMock = new Mock<ILoginHistoryRepository>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _options = new DainnUserOptions();

        _unitOfWorkMock.SetupGet(u => u.LoginHistories).Returns(_loginHistoryRepositoryMock.Object);
        _unitOfWorkMock.SetupGet(u => u.Sessions).Returns(_sessionRepositoryMock.Object);

        _authenticationService = new AuthenticationService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenServiceMock.Object,
            _options);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_CreatesUserAndSendsEmail()
    {
        // Arrange
        var email = "test@example.com";
        var username = "testuser";
        var password = "Test123!@#";
        var hashedPassword = "hashed_password";

        _userRepositoryMock.Setup(x => x.IsEmailTakenAsync(email, null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(username, null, default))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), password))
            .Returns(hashedPassword);
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendEmailVerificationAsync(email, username, It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        // Act
        var userId = await _authenticationService.RegisterAsync(email, username, password);

        // Assert
        userId.Should().NotBeEmpty();
        _userRepositoryMock.Verify(x => x.AddAsync(It.Is<User>(u =>
            u.Email == email.ToLowerInvariant() &&
            u.Username == username &&
            u.PasswordHash == hashedPassword &&
            u.Status == UserStatus.Pending &&
            !u.EmailVerified &&
            u.Tokens.Count == 1 &&
            u.Tokens.First().TokenType == TokenType.EmailVerification
        ), default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(email, username, It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "existing@example.com";
        var username = "testuser";
        var password = "Test123!@#";

        _userRepositoryMock.Setup(x => x.IsEmailTakenAsync(email, null, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _authenticationService.RegisterAsync(email, username, password);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email is already registered.");
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ThrowsInvalidOperationException()
    {
        // Arrange
        var email = "test@example.com";
        var username = "existinguser";
        var password = "Test123!@#";

        _userRepositoryMock.Setup(x => x.IsEmailTakenAsync(email, null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(username, null, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _authenticationService.RegisterAsync(email, username, password);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Username is already taken.");
        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_HashesPasswordCorrectly()
    {
        // Arrange
        var email = "test@example.com";
        var username = "testuser";
        var password = "Test123!@#";
        var hashedPassword = "hashed_password";

        _userRepositoryMock.Setup(x => x.IsEmailTakenAsync(email, null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(username, null, default))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), password))
            .Returns(hashedPassword);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _authenticationService.RegisterAsync(email, username, password);

        // Assert
        _passwordHasherMock.Verify(x => x.HashPassword(It.Is<User>(u => u.Email == email.ToLowerInvariant()), password), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_GeneratesSecureToken()
    {
        // Arrange
        var email = "test@example.com";
        var username = "testuser";
        var password = "Test123!@#";

        _userRepositoryMock.Setup(x => x.IsEmailTakenAsync(email, null, default))
            .ReturnsAsync(false);
        _userRepositoryMock.Setup(x => x.IsUsernameTakenAsync(username, null, default))
            .ReturnsAsync(false);
        _passwordHasherMock.Setup(x => x.HashPassword(It.IsAny<User>(), password))
            .Returns("hashed");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        User? capturedUser = null;
        _userRepositoryMock.Setup(x => x.AddAsync(It.IsAny<User>(), default))
            .Callback<User, CancellationToken>((user, _) => capturedUser = user)
            .Returns(Task.CompletedTask);

        // Act
        await _authenticationService.RegisterAsync(email, username, password);

        // Assert
        capturedUser.Should().NotBeNull();
        capturedUser!.Tokens.Should().HaveCount(1);
        var token = capturedUser.Tokens.First();
        token.TokenType.Should().Be(TokenType.EmailVerification);
        token.TokenValue.Should().NotBeNullOrEmpty();
        token.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(24), TimeSpan.FromMinutes(1));
        token.IsUsed.Should().BeFalse();
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyEmailAsync_WithValidToken_ActivatesUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "valid_token";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            EmailVerified = false,
            Status = UserStatus.Pending
        };
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenType = TokenType.EmailVerification,
            TokenValue = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            IsRevoked = false
        });

        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(userId, default))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _authenticationService.VerifyEmailAsync(userId, token);

        // Assert
        result.Should().BeTrue();
        user.EmailVerified.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        user.Tokens.First().IsUsed.Should().BeTrue();
        user.Tokens.First().UsedAt.Should().NotBeNull();
        _userRepositoryMock.Verify(x => x.Update(user), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "expired_token";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            EmailVerified = false,
            Status = UserStatus.Pending
        };
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenType = TokenType.EmailVerification,
            TokenValue = token,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false,
            IsRevoked = false
        });

        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.VerifyEmailAsync(userId, token);

        // Assert
        result.Should().BeFalse();
        user.EmailVerified.Should().BeFalse();
        user.Status.Should().Be(UserStatus.Pending);
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task VerifyEmailAsync_WithUsedToken_ReturnsFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = "used_token";
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            EmailVerified = false,
            Status = UserStatus.Pending
        };
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenType = TokenType.EmailVerification,
            TokenValue = token,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true, // Already used
            IsRevoked = false
        });

        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(userId, default))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.VerifyEmailAsync(userId, token);

        // Assert
        result.Should().BeFalse();
        _userRepositoryMock.Verify(x => x.Update(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WithUnverifiedUser_SendsNewEmail()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = "testuser",
            EmailVerified = false,
            Status = UserStatus.Pending
        };
        var oldToken = new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.EmailVerification,
            TokenValue = "old_token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            IsRevoked = false
        };
        user.Tokens.Add(oldToken);

        _userRepositoryMock.Setup(x => x.GetByEmailWithTokensAsync(email, default))
            .ReturnsAsync(user);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _authenticationService.ResendVerificationEmailAsync(email);

        // Assert
        result.Should().BeTrue();
        oldToken.IsRevoked.Should().BeTrue();
        oldToken.RevokedAt.Should().NotBeNull();
        user.Tokens.Should().HaveCount(2);
        var newToken = user.Tokens.Last();
        newToken.TokenType.Should().Be(TokenType.EmailVerification);
        newToken.IsUsed.Should().BeFalse();
        newToken.IsRevoked.Should().BeFalse();
        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(email, user.Username, It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WithVerifiedUser_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = "testuser",
            EmailVerified = true, // Already verified
            Status = UserStatus.Active
        };

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, default))
            .ReturnsAsync(user);

        // Act
        var result = await _authenticationService.ResendVerificationEmailAsync(email);

        // Assert
        result.Should().BeFalse();
        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task ResendVerificationEmailAsync_WithNonExistentUser_ReturnsFalse()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _userRepositoryMock.Setup(x => x.GetByEmailAsync(email, default))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _authenticationService.ResendVerificationEmailAsync(email);

        // Assert
        result.Should().BeFalse();
        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    // -- LoginAsync --

    private User CreateActiveUser(
        string email = "user@example.com",
        string username = "testuser",
        string passwordHash = "hashed",
        bool emailVerified = true,
        UserStatus status = UserStatus.Active)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            PasswordHash = passwordHash,
            EmailVerified = emailVerified,
            Status = status,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokensAndCreatesSession()
    {
        // Arrange
        var user = CreateActiveUser(email: "login@example.com");
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync("login@example.com", default))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "Pass123!@#"))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(user, It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("access-jwt", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("refresh-plain");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("refresh-plain")).Returns("refresh-hash");

        // Act
        var result = await _authenticationService.LoginAsync("login@example.com", "Pass123!@#", "127.0.0.1", "ua");

        // Assert
        result.AccessToken.Should().Be("access-jwt");
        result.RefreshToken.Should().Be("refresh-plain");
        result.SessionId.Should().NotBeEmpty();
        result.User.Id.Should().Be(user.Id);
        result.User.Email.Should().Be("login@example.com");

        // Stored refresh token must be hashed, never plain.
        _userRepositoryMock.Verify(r => r.AddTokenAsync(It.Is<UserToken>(t =>
            t.UserId == user.Id && t.TokenType == TokenType.RefreshToken && t.TokenValue == "refresh-hash"), default), Times.Once);
        _sessionRepositoryMock.Verify(s => s.AddAsync(It.Is<UserSession>(us =>
            us.UserId == user.Id && us.SessionToken == "refresh-hash" && us.IsActive), default), Times.Once);
        _loginHistoryRepositoryMock.Verify(l => l.AddAsync(It.Is<LoginHistory>(h =>
            h.UserId == user.Id && h.IsSuccessful), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);

        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        user.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_NormalizesEmailBeforeLookup()
    {
        // Arrange
        var user = CreateActiveUser(email: "user@example.com");
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync("user@example.com", default))
            .ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("jwt", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("rt");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("rt")).Returns("rt-hash");

        // Act
        await _authenticationService.LoginAsync("  USER@Example.COM  ", "Pass123!@#", null, null);

        // Assert
        _userRepositoryMock.Verify(x => x.GetByEmailWithRolesAsync("user@example.com", default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsInvalidCredentials()
    {
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(It.IsAny<string>(), default))
            .ReturnsAsync((User?)null);

        var act = async () => await _authenticationService.LoginAsync("unknown@example.com", "Pass123!@#", null, null);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_LogsFailureAndIncrementsCounter()
    {
        var user = CreateActiveUser();
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        var act = async () => await _authenticationService.LoginAsync(user.Email, "wrong", "127.0.0.1", "ua");

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        user.FailedLoginAttempts.Should().Be(1);
        _loginHistoryRepositoryMock.Verify(l => l.AddAsync(It.Is<LoginHistory>(h => !h.IsSuccessful), default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_AfterMaxFailures_LocksAccount()
    {
        var user = CreateActiveUser();
        user.FailedLoginAttempts = _options.MaxFailedLoginAttempts - 1;
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        var act = async () => await _authenticationService.LoginAsync(user.Email, "wrong", null, null);

        await act.Should().ThrowAsync<InvalidCredentialsException>();
        user.FailedLoginAttempts.Should().Be(_options.MaxFailedLoginAttempts);
        user.LockoutEnd.Should().NotBeNull();
        user.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_WhileLockedOut_ThrowsAccountLocked()
    {
        var user = CreateActiveUser();
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(5);
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);

        var act = async () => await _authenticationService.LoginAsync(user.Email, "any", null, null);

        await act.Should().ThrowAsync<AccountLockedException>();
        // Password verification should NOT happen while account is locked.
        _passwordHasherMock.Verify(p => p.VerifyHashedPassword(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _loginHistoryRepositoryMock.Verify(l => l.AddAsync(It.Is<LoginHistory>(h => !h.IsSuccessful), default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithExpiredLockout_AllowsLoginAndResetsLockout()
    {
        var user = CreateActiveUser();
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(-1); // Expired
        user.FailedLoginAttempts = 5;
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "Pass123!@#"))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("jwt", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("rt");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("rt")).Returns("rt-hash");

        await _authenticationService.LoginAsync(user.Email, "Pass123!@#", null, null);

        user.LockoutEnd.Should().BeNull();
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_ThrowsWhenVerificationRequired()
    {
        _options.RequireEmailVerification = true;
        var user = CreateActiveUser(emailVerified: false, status: UserStatus.Pending);
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "Pass123!@#"))
            .Returns(PasswordVerificationResult.Success);

        var act = async () => await _authenticationService.LoginAsync(user.Email, "Pass123!@#", null, null);

        await act.Should().ThrowAsync<EmailNotVerifiedException>();
        _jwtTokenServiceMock.Verify(j => j.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithUnverifiedEmail_AllowsLoginWhenVerificationDisabled()
    {
        _options.RequireEmailVerification = false;
        var user = CreateActiveUser(emailVerified: false, status: UserStatus.Active);
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "Pass123!@#"))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("jwt", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("rt");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("rt")).Returns("rt-hash");

        var result = await _authenticationService.LoginAsync(user.Email, "Pass123!@#", null, null);

        result.AccessToken.Should().Be("jwt");
    }

    [Theory]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deactivated)]
    [InlineData(UserStatus.Locked)]
    public async Task LoginAsync_WithInactiveStatus_ThrowsAccountInactive(UserStatus status)
    {
        var user = CreateActiveUser(status: status);
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "Pass123!@#"))
            .Returns(PasswordVerificationResult.Success);

        var act = async () => await _authenticationService.LoginAsync(user.Email, "Pass123!@#", null, null);

        await act.Should().ThrowAsync<AccountInactiveException>().Where(ex => ex.Status == status);
    }

    [Fact]
    public async Task LoginAsync_DoesNotCreateSessionWhenSessionManagementDisabled()
    {
        _options.EnableSessionManagement = false;
        var user = CreateActiveUser();
        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "Pass123!@#"))
            .Returns(PasswordVerificationResult.Success);
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("jwt", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("rt");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("rt")).Returns("rt-hash");

        await _authenticationService.LoginAsync(user.Email, "Pass123!@#", null, null);

        _sessionRepositoryMock.Verify(s => s.AddAsync(It.IsAny<UserSession>(), default), Times.Never);
    }

    // -- RefreshTokenAsync --

    private UserToken ActiveRefreshToken(Guid userId, string hash, DateTime? expiresAt = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TokenType = TokenType.RefreshToken,
        TokenValue = hash,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
        IsUsed = false,
        IsRevoked = false,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task RefreshTokenAsync_WithEmptyToken_ThrowsInvalid()
    {
        var act = async () => await _authenticationService.RefreshTokenAsync("", null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithUnknownToken_ThrowsInvalid()
    {
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("hash");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("hash", default)).ReturnsAsync((UserToken?)null);

        var act = async () => await _authenticationService.RefreshTokenAsync("plain", null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithRevokedToken_ThrowsInvalid()
    {
        var user = CreateActiveUser();
        var token = ActiveRefreshToken(user.Id, "old-rt-hash");
        token.IsRevoked = true;

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);

        var act = async () => await _authenticationService.RefreshTokenAsync("plain", null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_WithExpiredToken_ThrowsInvalid()
    {
        var user = CreateActiveUser();
        var token = ActiveRefreshToken(user.Id, "old-rt-hash", expiresAt: DateTime.UtcNow.AddMinutes(-1));

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);

        var act = async () => await _authenticationService.RefreshTokenAsync("plain", null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_OnReuse_RevokesAllAndDeactivatesSessions()
    {
        var user = CreateActiveUser();
        var token = ActiveRefreshToken(user.Id, "old-rt-hash");
        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow.AddMinutes(-5);

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);

        var act = async () => await _authenticationService.RefreshTokenAsync("plain", null, null);
        var ex = (await act.Should().ThrowAsync<InvalidRefreshTokenException>()).Which;
        ex.IsReuseDetected.Should().BeTrue();

        _userRepositoryMock.Verify(r => r.RevokeAllRefreshTokensAsync(user.Id, default), Times.Once);
        _sessionRepositoryMock.Verify(s => s.DeactivateAllByUserIdAsync(user.Id, default), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_OnReuse_SkipsSessionDeactivationWhenSessionsDisabled()
    {
        _options.EnableSessionManagement = false;
        var user = CreateActiveUser();
        var token = ActiveRefreshToken(user.Id, "old-rt-hash");
        token.IsUsed = true;

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);

        var act = async () => await _authenticationService.RefreshTokenAsync("plain", null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();

        _userRepositoryMock.Verify(r => r.RevokeAllRefreshTokensAsync(user.Id, default), Times.Once);
        _sessionRepositoryMock.Verify(s => s.DeactivateAllByUserIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task RefreshTokenAsync_OnSuccess_RotatesTokenAndSession()
    {
        var user = CreateActiveUser();
        var token = ActiveRefreshToken(user.Id, "old-rt-hash");
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = "old-rt-hash",
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(6)
        };

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("new-rt")).Returns("new-rt-hash");
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), session.Id))
            .Returns(new AccessTokenResult("new-access-jwt", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new-rt");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default)).ReturnsAsync(user);
        _sessionRepositoryMock.Setup(s => s.GetByTokenAsync("old-rt-hash", default)).ReturnsAsync(session);

        var result = await _authenticationService.RefreshTokenAsync("plain", "1.2.3.4", "ua");

        result.AccessToken.Should().Be("new-access-jwt");
        result.RefreshToken.Should().Be("new-rt");
        result.SessionId.Should().Be(session.Id); // same session, rotated

        token.IsUsed.Should().BeTrue(); // old token marked used
        token.UsedAt.Should().NotBeNull();

        _userRepositoryMock.Verify(r => r.AddTokenAsync(It.Is<UserToken>(t =>
            t.TokenType == TokenType.RefreshToken && t.TokenValue == "new-rt-hash"), default), Times.Once);

        session.SessionToken.Should().Be("new-rt-hash"); // session rotated in-place
        session.IpAddress.Should().Be("1.2.3.4");
        session.UserAgent.Should().Be("ua");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithMissingSession_CreatesNewSessionDefensively()
    {
        var user = CreateActiveUser();
        var token = ActiveRefreshToken(user.Id, "old-rt-hash");

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("new-rt")).Returns("new-rt-hash");
        _jwtTokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Returns(new AccessTokenResult("new-access", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("new-rt");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default)).ReturnsAsync(user);
        _sessionRepositoryMock.Setup(s => s.GetByTokenAsync("old-rt-hash", default)).ReturnsAsync((UserSession?)null);

        await _authenticationService.RefreshTokenAsync("plain", null, null);

        _sessionRepositoryMock.Verify(s => s.AddAsync(It.Is<UserSession>(us =>
            us.UserId == user.Id && us.SessionToken == "new-rt-hash" && us.IsActive), default), Times.Once);
    }

    [Theory]
    [InlineData(UserStatus.Suspended)]
    [InlineData(UserStatus.Deactivated)]
    [InlineData(UserStatus.Locked)]
    public async Task RefreshTokenAsync_WithInactiveAccount_ThrowsAccountInactive(UserStatus status)
    {
        var user = CreateActiveUser(status: status);
        var token = ActiveRefreshToken(user.Id, "old-rt-hash");

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default)).ReturnsAsync(user);

        var act = async () => await _authenticationService.RefreshTokenAsync("plain", null, null);
        await act.Should().ThrowAsync<AccountInactiveException>().Where(ex => ex.Status == status);
    }

    [Fact]
    public async Task RefreshTokenAsync_WhileLockedOut_ThrowsAccountLocked()
    {
        var user = CreateActiveUser();
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(5);
        var token = ActiveRefreshToken(user.Id, "old-rt-hash");

        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("plain")).Returns("old-rt-hash");
        _userRepositoryMock.Setup(x => x.GetRefreshTokenByHashAsync("old-rt-hash", default)).ReturnsAsync(token);
        _userRepositoryMock.Setup(x => x.GetWithRolesAsync(user.Id, default)).ReturnsAsync(user);

        var act = async () => await _authenticationService.RefreshTokenAsync("plain", null, null);
        await act.Should().ThrowAsync<AccountLockedException>();
    }

    // -- UnlockAccountAsync --

    [Fact]
    public async Task UnlockAccountAsync_WithEmptyUserId_ReturnsFalse()
    {
        var result = await _authenticationService.UnlockAccountAsync(Guid.Empty);

        result.Should().BeFalse();
        _userRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
    }

    [Fact]
    public async Task UnlockAccountAsync_WithUnknownUser_ReturnsFalse()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId, default)).ReturnsAsync((User?)null);

        var result = await _authenticationService.UnlockAccountAsync(userId);

        result.Should().BeFalse();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task UnlockAccountAsync_ResetsCountersAndClearsLockout()
    {
        var user = CreateActiveUser();
        user.FailedLoginAttempts = 5;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _authenticationService.UnlockAccountAsync(user.Id);

        result.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UnlockAccountAsync_RestoresLockedStatusToActive()
    {
        var user = CreateActiveUser(status: UserStatus.Locked);
        user.FailedLoginAttempts = 5;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(10);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        await _authenticationService.UnlockAccountAsync(user.Id);

        user.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task UnlockAccountAsync_DoesNotChangeNonLockedStatuses()
    {
        // Suspended/Deactivated must NOT silently become Active just because we unlock the
        // login counter — those are separate admin decisions.
        var user = CreateActiveUser(status: UserStatus.Suspended);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        await _authenticationService.UnlockAccountAsync(user.Id);

        user.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public async Task UnlockAccountAsync_OnUnlockedUser_IsNoOpButReturnsTrue()
    {
        // Idempotent: existing user, already-unlocked state, should still report success.
        var user = CreateActiveUser();
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id, default)).ReturnsAsync(user);

        var result = await _authenticationService.UnlockAccountAsync(user.Id);

        result.Should().BeTrue();
    }

    // -- Lockout email notification --

    [Fact]
    public async Task LoginAsync_OnNewLockout_SendsLockoutEmail()
    {
        _options.MaxFailedLoginAttempts = 3;
        var user = CreateActiveUser();
        user.FailedLoginAttempts = 2; // Next failure crosses threshold.

        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        var act = async () => await _authenticationService.LoginAsync(user.Email, "wrong", null, null);
        await act.Should().ThrowAsync<InvalidCredentialsException>();

        _emailServiceMock.Verify(e => e.SendAccountLockoutNotificationAsync(
            user.Email, user.Username, It.IsAny<DateTime>(), default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_OnEmailSendFailure_StillCompletesLoginFailureFlow()
    {
        // Email dispatch must never break the login pipeline. The exception path remains
        // InvalidCredentialsException, lockout state still persisted.
        _options.MaxFailedLoginAttempts = 1;
        var user = CreateActiveUser();

        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);
        _emailServiceMock.Setup(e => e.SendAccountLockoutNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), default))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var act = async () => await _authenticationService.LoginAsync(user.Email, "wrong", null, null);
        await act.Should().ThrowAsync<InvalidCredentialsException>();

        user.LockoutEnd.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_FailureBelowThreshold_DoesNotSendLockoutEmail()
    {
        _options.MaxFailedLoginAttempts = 5;
        var user = CreateActiveUser();
        user.FailedLoginAttempts = 0;

        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "wrong"))
            .Returns(PasswordVerificationResult.Failed);

        var act = async () => await _authenticationService.LoginAsync(user.Email, "wrong", null, null);
        await act.Should().ThrowAsync<InvalidCredentialsException>();

        _emailServiceMock.Verify(e => e.SendAccountLockoutNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), default), Times.Never);
    }

    // -- LogoutAsync --

    [Fact]
    public async Task LogoutAsync_WithEmptySessionId_IsNoOp()
    {
        await _authenticationService.LogoutAsync(Guid.Empty);

        _sessionRepositoryMock.Verify(s => s.GetByIdAsync(It.IsAny<Guid>(), default), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_WithUnknownSession_IsNoOp()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepositoryMock.Setup(s => s.GetByIdAsync(sessionId, default)).ReturnsAsync((UserSession?)null);

        await _authenticationService.LogoutAsync(sessionId);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_DeactivatesSessionAndRevokesRefreshToken()
    {
        var sessionId = Guid.NewGuid();
        var session = new UserSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            SessionToken = "rt-hash",
            IsActive = true
        };
        var refreshToken = new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            TokenType = TokenType.RefreshToken,
            TokenValue = "rt-hash",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsUsed = false,
            IsRevoked = false
        };

        _sessionRepositoryMock.Setup(s => s.GetByIdAsync(sessionId, default)).ReturnsAsync(session);
        _userRepositoryMock.Setup(r => r.GetRefreshTokenByHashAsync("rt-hash", default)).ReturnsAsync(refreshToken);

        await _authenticationService.LogoutAsync(sessionId);

        session.IsActive.Should().BeFalse();
        refreshToken.IsRevoked.Should().BeTrue();
        refreshToken.RevokedAt.Should().NotBeNull();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithAlreadyUsedToken_DoesNotRewriteRevokeTimestamp()
    {
        // If the refresh token is already used (rotated), don't mark it revoked — leave audit trail intact.
        var sessionId = Guid.NewGuid();
        var session = new UserSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            SessionToken = "rt-hash",
            IsActive = true
        };
        var refreshToken = new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = session.UserId,
            TokenType = TokenType.RefreshToken,
            TokenValue = "rt-hash",
            IsUsed = true,
            UsedAt = DateTime.UtcNow.AddMinutes(-5),
            IsRevoked = false
        };

        _sessionRepositoryMock.Setup(s => s.GetByIdAsync(sessionId, default)).ReturnsAsync(session);
        _userRepositoryMock.Setup(r => r.GetRefreshTokenByHashAsync("rt-hash", default)).ReturnsAsync(refreshToken);

        await _authenticationService.LogoutAsync(sessionId);

        refreshToken.IsRevoked.Should().BeFalse(); // already used; not "revoked"
        refreshToken.RevokedAt.Should().BeNull();
        session.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_IsIdempotent_OnInactiveSession()
    {
        var sessionId = Guid.NewGuid();
        var session = new UserSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            SessionToken = "rt-hash",
            IsActive = false
        };
        _sessionRepositoryMock.Setup(s => s.GetByIdAsync(sessionId, default)).ReturnsAsync(session);
        _userRepositoryMock.Setup(r => r.GetRefreshTokenByHashAsync("rt-hash", default)).ReturnsAsync((UserToken?)null);

        await _authenticationService.LogoutAsync(sessionId);

        // No state change but SaveChanges still called (cheap; keeps logic uniform).
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_ReturnsRolesFromUser()
    {
        var user = CreateActiveUser();
        var role1 = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var role2 = new Role { Id = Guid.NewGuid(), Name = "User" };
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role1.Id, Role = role1 });
        user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role2.Id, Role = role2 });

        _userRepositoryMock.Setup(x => x.GetByEmailWithRolesAsync(user.Email, default)).ReturnsAsync(user);
        _passwordHasherMock.Setup(x => x.VerifyHashedPassword(user, user.PasswordHash, "Pass123!@#"))
            .Returns(PasswordVerificationResult.Success);
        IEnumerable<string>? capturedRoles = null;
        _jwtTokenServiceMock
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<IEnumerable<string>>(), It.IsAny<Guid>()))
            .Callback<User, IEnumerable<string>, Guid>((_, roles, _) => capturedRoles = roles.ToList())
            .Returns(new AccessTokenResult("jwt", DateTime.UtcNow.AddMinutes(60)));
        _jwtTokenServiceMock.Setup(x => x.GenerateRefreshToken()).Returns("rt");
        _jwtTokenServiceMock.Setup(x => x.HashRefreshToken("rt")).Returns("rt-hash");

        var result = await _authenticationService.LoginAsync(user.Email, "Pass123!@#", null, null);

        capturedRoles.Should().BeEquivalentTo(new[] { "Admin", "User" });
        result.User.Roles.Should().BeEquivalentTo(new[] { "Admin", "User" });
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ForgotPasswordAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_UnknownEmail_CompletesWithoutError()
    {
        // No user enumeration: unknown email must be a silent no-op.
        _userRepositoryMock.Setup(x => x.GetByEmailWithTokensAsync(It.IsAny<string>(), default))
            .ReturnsAsync((User?)null);

        var act = async () => await _authenticationService.ForgotPasswordAsync("ghost@example.com");

        await act.Should().NotThrowAsync();
        _emailServiceMock.Verify(
            x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPasswordAsync_KnownEmail_StoresHashedTokenAndSendsEmail()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "known@example.com",
            Username = "known",
            Status = Core.Enums.UserStatus.Active,
            PasswordHash = "hash"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailWithTokensAsync("known@example.com", default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        await _authenticationService.ForgotPasswordAsync("known@example.com");

        // Token stored as hash (not plain-text).
        _userRepositoryMock.Verify(x => x.AddTokenAsync(It.Is<UserToken>(t =>
            t.TokenType == TokenType.PasswordReset &&
            !t.IsUsed &&
            !t.IsRevoked &&
            t.TokenValue != null // value is the SHA-256 hex, not empty
        ), default), Times.Once);

        _emailServiceMock.Verify(x => x.SendPasswordResetAsync(
            "known@example.com", "known", It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task ForgotPasswordAsync_SmtpFailure_DoesNotBubbleException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "smtp-fail@example.com",
            Username = "smtpfail",
            Status = Core.Enums.UserStatus.Active,
            PasswordHash = "hash"
        };

        _userRepositoryMock.Setup(x => x.GetByEmailWithTokensAsync("smtp-fail@example.com", default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .ThrowsAsync(new Exception("SMTP failure"));

        // SMTP failure must be swallowed — caller gets success.
        var act = async () => await _authenticationService.ForgotPasswordAsync("smtp-fail@example.com");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ForgotPasswordAsync_RevokesPreviousActiveResetTokens()
    {
        var oldToken = new UserToken
        {
            Id = Guid.NewGuid(),
            TokenType = TokenType.PasswordReset,
            TokenValue = "old-hash",
            IsUsed = false,
            IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "revoke@example.com",
            Username = "revoke",
            Status = Core.Enums.UserStatus.Active,
            PasswordHash = "hash"
        };
        user.Tokens.Add(oldToken);

        _userRepositoryMock.Setup(x => x.GetByEmailWithTokensAsync("revoke@example.com", default))
            .ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        await _authenticationService.ForgotPasswordAsync("revoke@example.com");

        oldToken.IsRevoked.Should().BeTrue();
        oldToken.RevokedAt.Should().NotBeNull();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ResetPasswordAsync
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResetPasswordAsync_NullOrWhitespaceToken_ThrowsInvalidPasswordResetTokenException()
    {
        var act = async () => await _authenticationService.ResetPasswordAsync("   ", "New-Pass-1!");
        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_TokenNotFound_ThrowsInvalidPasswordResetTokenException()
    {
        _userRepositoryMock.Setup(x => x.GetPasswordResetTokenByHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync((UserToken?)null);

        var act = async () => await _authenticationService.ResetPasswordAsync("bad-token", "New-Pass-1!");
        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_AlreadyUsedToken_ThrowsInvalidPasswordResetTokenException()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@e.com", Username = "u", PasswordHash = "h" };
        var token = new UserToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, User = user,
            TokenType = TokenType.PasswordReset,
            TokenValue = "hash",
            IsUsed = true, IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _userRepositoryMock.Setup(x => x.GetPasswordResetTokenByHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        var act = async () => await _authenticationService.ResetPasswordAsync("some-token", "New-Pass-1!");
        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_ExpiredToken_ThrowsInvalidPasswordResetTokenException()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "u@e.com", Username = "u", PasswordHash = "h" };
        var token = new UserToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, User = user,
            TokenType = TokenType.PasswordReset,
            TokenValue = "hash",
            IsUsed = false, IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // expired
        };
        _userRepositoryMock.Setup(x => x.GetPasswordResetTokenByHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);

        var act = async () => await _authenticationService.ResetPasswordAsync("some-token", "New-Pass-1!");
        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidToken_UpdatesPasswordAndRevokesRefreshTokens()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "reset@example.com", Username = "resetter", PasswordHash = "old-hash" };
        var token = new UserToken
        {
            Id = Guid.NewGuid(), UserId = user.Id, User = user,
            TokenType = TokenType.PasswordReset,
            TokenValue = "token-hash",
            IsUsed = false, IsRevoked = false,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock.Setup(x => x.GetPasswordResetTokenByHashAsync(It.IsAny<string>(), default))
            .ReturnsAsync(token);
        _passwordHasherMock.Setup(x => x.HashPassword(user, "New-Pass-1!"))
            .Returns("new-hash");
        _userRepositoryMock.Setup(x => x.RevokeAllRefreshTokensAsync(user.Id, default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _emailServiceMock.Setup(x => x.SendPasswordChangedNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        await _authenticationService.ResetPasswordAsync("plain-token", "New-Pass-1!");

        // Token is consumed.
        token.IsUsed.Should().BeTrue();
        token.UsedAt.Should().NotBeNull();

        // Password is updated.
        user.PasswordHash.Should().Be("new-hash");

        // Refresh tokens are revoked.
        _userRepositoryMock.Verify(x => x.RevokeAllRefreshTokensAsync(user.Id, default), Times.Once);

        // Confirmation email sent.
        _emailServiceMock.Verify(x => x.SendPasswordChangedNotificationAsync(
            "reset@example.com", "resetter", default), Times.Once);
    }
}
