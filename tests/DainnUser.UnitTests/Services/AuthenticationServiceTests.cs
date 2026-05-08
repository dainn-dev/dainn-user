using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace DainnUser.UnitTests.Services;

public class AuthenticationServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IPasswordHasher<User>> _passwordHasherMock;
    private readonly AuthenticationService _authenticationService;

    public AuthenticationServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _emailServiceMock = new Mock<IEmailService>();
        _passwordHasherMock = new Mock<IPasswordHasher<User>>();

        _authenticationService = new AuthenticationService(
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _emailServiceMock.Object,
            _passwordHasherMock.Object);
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
}
