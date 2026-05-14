using DainnUser.Core.Configuration;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Repositories;
using DainnUser.Infrastructure.Services;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace DainnUser.IntegrationTests.Services;

public class AuthenticationServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private const string TestJwtSecret = "test-secret-must-be-at-least-32-bytes-long-please-okay";

    private readonly DatabaseFixture _fixture;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly DainnUserOptions _options;
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
        _options = new DainnUserOptions { RequireEmailVerification = false };
        _jwtTokenService = new JwtTokenService(
            Options.Create(new JwtOptions { Secret = TestJwtSecret }),
            _options);

        _authenticationService = new AuthenticationService(
            _userRepository,
            _unitOfWork,
            _emailServiceMock.Object,
            _passwordHasher,
            _jwtTokenService,
            _options);
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

    // -- LoginAsync end-to-end --

    [Fact]
    public async Task LoginAsync_EndToEnd_IssuesTokensAndPersistsSession()
    {
        // Arrange: register only (RequireEmailVerification=false in this fixture so verify isn't needed).
        var email = "login-e2e@example.com";
        var username = "logine2e";
        var password = "Test123!@#";

        var userId = await _authenticationService.RegisterAsync(email, username, password);

        // Detach tracked entities so LoginAsync starts from a clean tracking state.
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await _authenticationService.LoginAsync(email, password, "10.0.0.1", "test-ua");

        // Assert
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Id.Should().Be(userId);
        result.User.Email.Should().Be(email);

        // Refresh token must be hashed in storage, never plain.
        var stored = await _fixture.DbContext.UserTokens
            .Where(t => t.UserId == userId && t.TokenType == TokenType.RefreshToken)
            .ToListAsync();
        stored.Should().ContainSingle();
        stored[0].TokenValue.Should().NotBe(result.RefreshToken);
        stored[0].TokenValue.Should().Be(_jwtTokenService.HashRefreshToken(result.RefreshToken));

        // Session created and tied to the same hash.
        var session = await _fixture.DbContext.UserSessions.SingleAsync(s => s.Id == result.SessionId);
        session.SessionToken.Should().Be(stored[0].TokenValue);
        session.IsActive.Should().BeTrue();
        session.IpAddress.Should().Be("10.0.0.1");

        // Login history records success.
        var history = await _fixture.DbContext.LoginHistories.SingleAsync(h => h.UserId == userId);
        history.IsSuccessful.Should().BeTrue();

        // Lockout counters reset, last-login stamped.
        var userAfter = await _fixture.DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        userAfter.FailedLoginAttempts.Should().Be(0);
        userAfter.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_RecordsFailureAndIncrementsCounter()
    {
        var email = "fail-e2e@example.com";
        var userId = await _authenticationService.RegisterAsync(email, "faile2e", "Correct123!@#");
        var registered = await _fixture.DbContext.Users.Include(u => u.Tokens).FirstAsync(u => u.Id == userId);
        await _authenticationService.VerifyEmailAsync(userId, registered.Tokens.First().TokenValue);
        _fixture.DbContext.ChangeTracker.Clear();

        var act = async () => await _authenticationService.LoginAsync(email, "Wrong-Password!", "1.2.3.4", "ua");
        await act.Should().ThrowAsync<InvalidCredentialsException>();

        var userAfter = await _fixture.DbContext.Users.AsNoTracking().FirstAsync(u => u.Id == userId);
        userAfter.FailedLoginAttempts.Should().Be(1);

        var history = await _fixture.DbContext.LoginHistories.AsNoTracking().Where(h => h.UserId == userId).ToListAsync();
        history.Should().ContainSingle().Which.IsSuccessful.Should().BeFalse();
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsGenericInvalidCredentials()
    {
        var act = async () => await _authenticationService.LoginAsync("ghost@nowhere.example", "anything", null, null);
        await act.Should().ThrowAsync<InvalidCredentialsException>();
    }

    [Fact]
    public async Task RefreshTokenAsync_EndToEnd_RotatesTokenAndSession()
    {
        // Register + login to get a refresh token.
        var email = "refresh-e2e@example.com";
        var password = "Test123!@#";
        await _authenticationService.RegisterAsync(email, "refreshe2e", password);
        _fixture.DbContext.ChangeTracker.Clear();

        var login = await _authenticationService.LoginAsync(email, password, "1.1.1.1", "first-ua");
        _fixture.DbContext.ChangeTracker.Clear();

        var oldRefreshToken = login.RefreshToken;
        var oldHash = _jwtTokenService.HashRefreshToken(oldRefreshToken);

        // Act
        var refreshed = await _authenticationService.RefreshTokenAsync(oldRefreshToken, "2.2.2.2", "second-ua");

        // Assert: new tokens issued, session preserved.
        refreshed.AccessToken.Should().NotBeNullOrEmpty();
        refreshed.RefreshToken.Should().NotBe(oldRefreshToken);
        refreshed.SessionId.Should().Be(login.SessionId);

        // Old token is now marked used.
        var oldTokenInDb = await _fixture.DbContext.UserTokens.AsNoTracking()
            .SingleAsync(t => t.TokenValue == oldHash);
        oldTokenInDb.IsUsed.Should().BeTrue();
        oldTokenInDb.UsedAt.Should().NotBeNull();

        // New token is stored hashed and active.
        var newHash = _jwtTokenService.HashRefreshToken(refreshed.RefreshToken);
        var newTokenInDb = await _fixture.DbContext.UserTokens.AsNoTracking()
            .SingleAsync(t => t.TokenValue == newHash);
        newTokenInDb.TokenType.Should().Be(TokenType.RefreshToken);
        newTokenInDb.IsUsed.Should().BeFalse();
        newTokenInDb.IsRevoked.Should().BeFalse();

        // Session rotated to the new hash and metadata updated.
        var session = await _fixture.DbContext.UserSessions.AsNoTracking()
            .SingleAsync(s => s.Id == refreshed.SessionId);
        session.SessionToken.Should().Be(newHash);
        session.IpAddress.Should().Be("2.2.2.2");
        session.UserAgent.Should().Be("second-ua");
        session.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshTokenAsync_OnReuse_RevokesAllRefreshTokensAndSessions()
    {
        var email = "reuse-e2e@example.com";
        var password = "Test123!@#";
        await _authenticationService.RegisterAsync(email, "reusee2e", password);
        _fixture.DbContext.ChangeTracker.Clear();

        var login = await _authenticationService.LoginAsync(email, password, null, null);
        _fixture.DbContext.ChangeTracker.Clear();

        // First refresh: legitimate.
        await _authenticationService.RefreshTokenAsync(login.RefreshToken, null, null);
        _fixture.DbContext.ChangeTracker.Clear();

        // Replay the original (now-used) token — should trip reuse detection.
        var act = async () => await _authenticationService.RefreshTokenAsync(login.RefreshToken, null, null);
        var ex = (await act.Should().ThrowAsync<InvalidRefreshTokenException>()).Which;
        ex.IsReuseDetected.Should().BeTrue();

        // All refresh tokens for this user must be revoked or used.
        var userId = login.User.Id;
        var refreshTokens = await _fixture.DbContext.UserTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.TokenType == TokenType.RefreshToken)
            .ToListAsync();
        refreshTokens.Should().NotBeEmpty();
        refreshTokens.Should().OnlyContain(t => t.IsRevoked || t.IsUsed);

        // Sessions deactivated.
        var sessions = await _fixture.DbContext.UserSessions.AsNoTracking()
            .Where(s => s.UserId == userId)
            .ToListAsync();
        sessions.Should().OnlyContain(s => !s.IsActive);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithUnknownToken_ThrowsInvalid()
    {
        var act = async () => await _authenticationService.RefreshTokenAsync("totally-fake-token", null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task UnlockAccountAsync_EndToEnd_ResetsLockoutAndRestoresStatus()
    {
        // Arrange: lock a user.
        var email = "unlock-e2e@example.com";
        var userId = await _authenticationService.RegisterAsync(email, "unlocke2e", "Right-Pass-1!");
        var user = await _fixture.DbContext.Users.SingleAsync(u => u.Id == userId);
        user.EmailVerified = true;
        user.Status = UserStatus.Locked;
        user.FailedLoginAttempts = 5;
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act: admin unlocks.
        var result = await _authenticationService.UnlockAccountAsync(userId);

        // Assert: state reset, status back to Active, login now possible.
        result.Should().BeTrue();
        var unlocked = await _fixture.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        unlocked.FailedLoginAttempts.Should().Be(0);
        unlocked.LockoutEnd.Should().BeNull();
        unlocked.Status.Should().Be(UserStatus.Active);

        var login = await _authenticationService.LoginAsync(email, "Right-Pass-1!", null, null);
        login.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LogoutAsync_EndToEnd_DeactivatesSessionAndRevokesRefreshToken()
    {
        var email = "logout-e2e@example.com";
        var password = "Test123!@#";
        await _authenticationService.RegisterAsync(email, "logoute2e", password);
        _fixture.DbContext.ChangeTracker.Clear();

        var login = await _authenticationService.LoginAsync(email, password, null, null);
        _fixture.DbContext.ChangeTracker.Clear();

        await _authenticationService.LogoutAsync(login.SessionId);

        var session = await _fixture.DbContext.UserSessions.AsNoTracking()
            .SingleAsync(s => s.Id == login.SessionId);
        session.IsActive.Should().BeFalse();

        var hash = _jwtTokenService.HashRefreshToken(login.RefreshToken);
        var token = await _fixture.DbContext.UserTokens.AsNoTracking()
            .SingleAsync(t => t.TokenValue == hash);
        token.IsRevoked.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();

        // After logout, the same refresh token must no longer be exchangeable.
        var act = async () => await _authenticationService.RefreshTokenAsync(login.RefreshToken, null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task LogoutAsync_WithUnknownSession_IsIdempotent()
    {
        // Should not throw.
        await _authenticationService.LogoutAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task LoginAsync_WhenEmailVerificationRequired_BlocksUnverifiedUser()
    {
        _options.RequireEmailVerification = true;
        try
        {
            var email = "unverified-e2e@example.com";
            await _authenticationService.RegisterAsync(email, "unverifiede2e", "Test123!@#");

            var act = async () => await _authenticationService.LoginAsync(email, "Test123!@#", null, null);

            await act.Should().ThrowAsync<EmailNotVerifiedException>();
        }
        finally
        {
            _options.RequireEmailVerification = false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // ForgotPasswordAsync / ResetPasswordAsync  (PSA-62)
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPasswordAsync_KnownEmail_PersistsHashedToken()
    {
        var email = "forgot@example.com";
        var userId = await _authenticationService.RegisterAsync(email, "forgotuser", "Test-123!");

        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Returns(Task.CompletedTask);

        await _authenticationService.ForgotPasswordAsync(email);

        _fixture.DbContext.ChangeTracker.Clear();

        var resetToken = await _fixture.DbContext.Set<UserToken>()
            .FirstOrDefaultAsync(t => t.UserId == userId && t.TokenType == TokenType.PasswordReset);

        resetToken.Should().NotBeNull();
        resetToken!.IsUsed.Should().BeFalse();
        resetToken.IsRevoked.Should().BeFalse();
        resetToken.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
        _emailServiceMock.Verify(
            x => x.SendPasswordResetAsync(email, "forgotuser", It.IsAny<string>(), default),
            Times.Once);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidFlow_UpdatesPasswordAndRevokesTokens()
    {
        var email = "reset-int@example.com";
        var userId = await _authenticationService.RegisterAsync(email, "resetuser", "Old-Pass-1!");

        // Activate the user.
        var user = await _fixture.DbContext.Users.FirstAsync(u => u.Id == userId);
        user.EmailVerified = true;
        user.Status = UserStatus.Active;
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Log in to create a refresh token + session.
        _emailServiceMock.Setup(x => x.SendPasswordChangedNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        var loginResult = await _authenticationService.LoginAsync(email, "Old-Pass-1!", null, null);
        _fixture.DbContext.ChangeTracker.Clear();

        // Capture the reset token delivered by email.
        string capturedToken = string.Empty;
        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Callback<string, string, string, CancellationToken>((_, _, t, _) => capturedToken = t)
            .Returns(Task.CompletedTask);

        await _authenticationService.ForgotPasswordAsync(email);
        _fixture.DbContext.ChangeTracker.Clear();

        capturedToken.Should().NotBeNullOrEmpty();

        await _authenticationService.ResetPasswordAsync(capturedToken, "New-Pass-99!");
        _fixture.DbContext.ChangeTracker.Clear();

        // Old password no longer works.
        var loginOldAct = async () => await _authenticationService.LoginAsync(email, "Old-Pass-1!", null, null);
        await loginOldAct.Should().ThrowAsync<InvalidCredentialsException>();

        // New password works.
        _fixture.DbContext.ChangeTracker.Clear();
        var loginNew = await _authenticationService.LoginAsync(email, "New-Pass-99!", null, null);
        loginNew.Should().NotBeNull();

        // Old refresh token is revoked.
        _fixture.DbContext.ChangeTracker.Clear();
        var refreshTokens = await _fixture.DbContext.Set<UserToken>()
            .Where(t => t.UserId == userId && t.TokenType == TokenType.RefreshToken)
            .ToListAsync();
        refreshTokens.Should().Contain(t => t.IsRevoked);
    }

    [Fact]
    public async Task ResetPasswordAsync_TokenReuse_ThrowsOnSecondUse()
    {
        var email = "reuse-int@example.com";
        await _authenticationService.RegisterAsync(email, "reuseuser", "Pass-111!");

        _emailServiceMock.Setup(x => x.SendPasswordChangedNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), default)).Returns(Task.CompletedTask);

        string capturedToken = string.Empty;
        _emailServiceMock.Setup(x => x.SendPasswordResetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default))
            .Callback<string, string, string, CancellationToken>((_, _, t, _) => capturedToken = t)
            .Returns(Task.CompletedTask);

        await _authenticationService.ForgotPasswordAsync(email);
        _fixture.DbContext.ChangeTracker.Clear();

        // First reset succeeds.
        await _authenticationService.ResetPasswordAsync(capturedToken, "Pass-222!");
        _fixture.DbContext.ChangeTracker.Clear();

        // Second reset with same token must fail.
        var act = async () => await _authenticationService.ResetPasswordAsync(capturedToken, "Pass-333!");
        await act.Should().ThrowAsync<InvalidPasswordResetTokenException>();
    }

    #region ChangePasswordAsync

    [Fact]
    public async Task ChangePasswordAsync_ValidCurrentPassword_UpdatesHashAndInvalidatesOtherSessions()
    {
        // Arrange — create user and two sessions (simulate login from two devices)
        var password = "OldPass1!";
        var userId = await _authenticationService.RegisterAsync(
            "changepw@example.com", "changepwuser", password);

        var loginResult1 = await _authenticationService.LoginAsync(
            "changepw@example.com", password, "1.1.1.1", "DeviceA");
        var loginResult2 = await _authenticationService.LoginAsync(
            "changepw@example.com", password, "2.2.2.2", "DeviceB");

        var currentSessionId = loginResult1.SessionId;

        // Act
        await _authenticationService.ChangePasswordAsync(
            userId, currentSessionId, password, "NewPass2!");

        // Assert — user's password updated in DB
        var userInDb = await _fixture.DbContext.Users.FindAsync(userId);
        userInDb.Should().NotBeNull();
        var newHash = userInDb!.PasswordHash;
        var hashResult = new PasswordHasher<User>().VerifyHashedPassword(userInDb, newHash, "NewPass2!");
        hashResult.Should().Be(PasswordVerificationResult.Success);

        // Assert — second session deactivated, first session still active
        var sessions = await _fixture.DbContext.UserSessions
            .Where(s => s.UserId == userId)
            .ToListAsync();
        sessions.Should().HaveCount(2);
        sessions.First(s => s.Id == currentSessionId).IsActive.Should().BeTrue();
        sessions.First(s => s.Id == loginResult2.SessionId).IsActive.Should().BeFalse();

        // Assert — activity log recorded
        var logs = await _fixture.DbContext.ActivityLogs.Where(l => l.UserId == userId).ToListAsync();
        logs.Should().Contain(l => l.ActivityType == ActivityType.PasswordChange);

        // Assert — confirmation email sent
        _emailServiceMock.Verify(x => x.SendPasswordChangedNotificationAsync(
            "changepw@example.com", "changepwuser", default), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_WrongCurrentPassword_ThrowsAndDoesNotUpdateDb()
    {
        // Arrange
        var password = "OriginalPass1!";
        var userId = await _authenticationService.RegisterAsync(
            "changepw-fail@example.com", "changepwfail", password);

        var loginResult = await _authenticationService.LoginAsync(
            "changepw-fail@example.com", password, null, null);

        var originalHash = (await _fixture.DbContext.Users.FindAsync(userId))!.PasswordHash;

        // Act
        var act = async () => await _authenticationService.ChangePasswordAsync(
            userId, loginResult.SessionId, "WrongPass1!", "NewPass2!");

        // Assert
        await act.Should().ThrowAsync<InvalidCurrentPasswordException>();

        var userInDb = await _fixture.DbContext.Users.FindAsync(userId);
        userInDb!.PasswordHash.Should().Be(originalHash);
    }

    [Fact]
    public async Task ChangePasswordAsync_AfterChange_OldPasswordNoLongerWorks()
    {
        // Arrange
        var oldPassword = "BeforeChange1!";
        var newPassword = "AfterChange2!";
        var userId = await _authenticationService.RegisterAsync(
            "changepw-verify@example.com", "changepwverify", oldPassword);

        var loginResult = await _authenticationService.LoginAsync(
            "changepw-verify@example.com", oldPassword, null, null);

        // Act
        await _authenticationService.ChangePasswordAsync(
            userId, loginResult.SessionId, oldPassword, newPassword);

        // Assert — old password rejected
        var actOld = async () => await _authenticationService.LoginAsync(
            "changepw-verify@example.com", oldPassword, null, null);
        await actOld.Should().ThrowAsync<InvalidCredentialsException>();

        // Assert — new password accepted
        var newLogin = await _authenticationService.LoginAsync(
            "changepw-verify@example.com", newPassword, null, null);
        newLogin.AccessToken.Should().NotBeNullOrEmpty();
    }

    #endregion
}
