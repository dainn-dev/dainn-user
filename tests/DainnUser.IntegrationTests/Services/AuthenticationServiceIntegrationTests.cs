using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Repositories;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DainnUser.IntegrationTests.Services;

public class AuthenticationServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly AuthenticationService _authenticationService;
    private readonly UserRepository _userRepository;
    private readonly UnitOfWork _unitOfWork;

    public AuthenticationServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase();

        _emailServiceMock = new Mock<IEmailService>();
        _passwordHasher = new PasswordHasher<User>();
        _userRepository = new UserRepository(_fixture.DbContext);
        _unitOfWork = new UnitOfWork(_fixture.DbContext);

        _authenticationService = new AuthenticationService(
            _userRepository,
            _unitOfWork,
            _emailServiceMock.Object,
            _passwordHasher);
    }

    [Fact]
    public async Task RegisterAsync_EndToEnd_CreatesUserInDatabase()
    {
        // Arrange
        var email = "integration@example.com";
        var username = "integrationuser";
        var password = "Test123!@#";

        // Act
        var userId = await _authenticationService.RegisterAsync(email, username, password);

        // Assert
        userId.Should().NotBeEmpty();

        var userInDb = await _fixture.DbContext.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        userInDb.Should().NotBeNull();
        userInDb!.Email.Should().Be(email.ToLowerInvariant());
        userInDb.Username.Should().Be(username);
        userInDb.EmailVerified.Should().BeFalse();
        userInDb.Status.Should().Be(UserStatus.Pending);
        userInDb.PasswordHash.Should().NotBeNullOrEmpty();
        userInDb.Tokens.Should().HaveCount(1);
        userInDb.Tokens.First().TokenType.Should().Be(TokenType.EmailVerification);
        userInDb.Tokens.First().IsUsed.Should().BeFalse();
        userInDb.Tokens.First().IsRevoked.Should().BeFalse();

        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(
            email,
            username,
            It.IsAny<string>(),
            default), Times.Once);
    }

    [Fact]
    public async Task VerifyEmailAsync_EndToEnd_UpdatesUserInDatabase()
    {
        // Arrange
        var email = "verify@example.com";
        var username = "verifyuser";
        var password = "Test123!@#";

        var userId = await _authenticationService.RegisterAsync(email, username, password);
        var userInDb = await _fixture.DbContext.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Id == userId);
        var token = userInDb!.Tokens.First().TokenValue;

        // Act
        var result = await _authenticationService.VerifyEmailAsync(userId, token);

        // Assert
        result.Should().BeTrue();

        var updatedUser = await _fixture.DbContext.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        updatedUser.Should().NotBeNull();
        updatedUser!.EmailVerified.Should().BeTrue();
        updatedUser.Status.Should().Be(UserStatus.Active);
        updatedUser.Tokens.First().IsUsed.Should().BeTrue();
        updatedUser.Tokens.First().UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_PreventsCreation()
    {
        // Arrange
        var email = "duplicate@example.com";
        var username1 = "user1";
        var username2 = "user2";
        var password = "Test123!@#";

        await _authenticationService.RegisterAsync(email, username1, password);

        // Act
        var act = async () => await _authenticationService.RegisterAsync(email, username2, password);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Email is already registered.");

        var usersInDb = await _fixture.DbContext.Users
            .Where(u => u.Email == email.ToLowerInvariant())
            .ToListAsync();

        usersInDb.Should().HaveCount(1);
        usersInDb.First().Username.Should().Be(username1);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_PreventsCreation()
    {
        // Arrange
        var email1 = "user1@example.com";
        var email2 = "user2@example.com";
        var username = "duplicateuser";
        var password = "Test123!@#";

        await _authenticationService.RegisterAsync(email1, username, password);

        // Act
        var act = async () => await _authenticationService.RegisterAsync(email2, username, password);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Username is already taken.");

        var usersInDb = await _fixture.DbContext.Users
            .Where(u => u.Username == username)
            .ToListAsync();

        usersInDb.Should().HaveCount(1);
        usersInDb.First().Email.Should().Be(email1.ToLowerInvariant());
    }

    [Fact(Skip = "InMemory database has issues with collection modifications. TODO: Investigate and fix or use real database for this test.")]
    public async Task ResendVerificationEmail_EndToEnd_RevokesOldTokens()
    {
        // Arrange
        var email = "resend@example.com";
        var username = "resenduser";
        var password = "Test123!@#";

        var userId = await _authenticationService.RegisterAsync(email, username, password);

        // Act
        var result = await _authenticationService.ResendVerificationEmailAsync(email);

        // Assert
        result.Should().BeTrue();

        // Query final state
        var userAfterResend = await _fixture.DbContext.Users
            .AsNoTracking()
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Id == userId);

        userAfterResend.Should().NotBeNull();
        userAfterResend!.Tokens.Should().HaveCount(2);

        // Should have one revoked token and one new token
        var revokedTokens = userAfterResend.Tokens.Where(t => t.IsRevoked).ToList();
        var activeTokens = userAfterResend.Tokens.Where(t => !t.IsRevoked && !t.IsUsed).ToList();

        revokedTokens.Should().HaveCount(1);
        revokedTokens.First().RevokedAt.Should().NotBeNull();

        activeTokens.Should().HaveCount(1);
        activeTokens.First().TokenType.Should().Be(TokenType.EmailVerification);

        _emailServiceMock.Verify(x => x.SendEmailVerificationAsync(
            email,
            username,
            It.IsAny<string>(),
            default), Times.Exactly(2)); // Once for register, once for resend
    }

    [Fact]
    public async Task VerifyEmailAsync_WithExpiredToken_DoesNotUpdateDatabase()
    {
        // Arrange
        var email = "expired@example.com";
        var username = "expireduser";
        var password = "Test123!@#";

        var userId = await _authenticationService.RegisterAsync(email, username, password);

        // Manually expire the token
        var user = await _fixture.DbContext.Users
            .Include(u => u.Tokens)
            .FirstOrDefaultAsync(u => u.Id == userId);
        var token = user!.Tokens.First();
        token.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        await _fixture.DbContext.SaveChangesAsync();

        // Act
        var result = await _authenticationService.VerifyEmailAsync(userId, token.TokenValue);

        // Assert
        result.Should().BeFalse();

        var userAfter = await _fixture.DbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        userAfter.Should().NotBeNull();
        userAfter!.EmailVerified.Should().BeFalse();
        userAfter.Status.Should().Be(UserStatus.Pending);
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashedCorrectly()
    {
        // Arrange
        var email = "hash@example.com";
        var username = "hashuser";
        var password = "Test123!@#";

        // Act
        var userId = await _authenticationService.RegisterAsync(email, username, password);

        // Assert
        var user = await _fixture.DbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId);

        user.Should().NotBeNull();
        user!.PasswordHash.Should().NotBe(password);
        user.PasswordHash.Should().NotBeNullOrEmpty();

        // Verify password can be verified
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        verificationResult.Should().Be(PasswordVerificationResult.Success);
    }
}
