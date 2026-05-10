using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using DainnUser.Core.Exceptions;
using DainnUser.SecurityTests.TestFixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DainnUser.SecurityTests.Owasp;

/// <summary>
/// OWASP A07:2021 — Identification and Authentication Failures.
/// Brute force protection, password policy, refresh-token reuse detection, no user enumeration.
/// </summary>
public class A07_AuthenticationFailureTests
{
    [Fact]
    public async Task UnknownEmailAndWrongPassword_ReturnIdenticalGenericError()
    {
        // No user enumeration: the response for "no such email" must be indistinguishable
        // from "wrong password".
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("real@example.com", "real", "Pass123!@#");

        var unknown = async () => await fx.AuthenticationService.LoginAsync("ghost@example.com", "anything", null, null);
        var wrongPwd = async () => await fx.AuthenticationService.LoginAsync("real@example.com", "wrong", null, null);

        var ex1 = (await unknown.Should().ThrowAsync<InvalidCredentialsException>()).Which;
        var ex2 = (await wrongPwd.Should().ThrowAsync<InvalidCredentialsException>()).Which;

        // Same exception type AND same message — no signal that distinguishes the cases.
        ex1.Message.Should().Be(ex2.Message);
    }

    [Fact]
    public async Task BruteForceLogin_TriggersLockoutAfterMaxAttempts()
    {
        using var fx = new SecurityTestFixture(o =>
        {
            o.MaxFailedLoginAttempts = 3;
            o.LockoutDurationMinutes = 5;
        });
        await fx.RegisterAndActivateAsync("victim@example.com", "victim", "Real-Pass-123!");

        // Three wrong attempts → fourth attempt sees AccountLockedException even with correct password.
        for (var i = 0; i < 3; i++)
        {
            try { await fx.AuthenticationService.LoginAsync("victim@example.com", "Wrong-Pass-123!", null, null); }
            catch (InvalidCredentialsException) { /* expected */ }
            fx.DbContext.ChangeTracker.Clear();
        }

        var lockedAct = async () => await fx.AuthenticationService.LoginAsync("victim@example.com", "Real-Pass-123!", null, null);
        await lockedAct.Should().ThrowAsync<AccountLockedException>();
    }

    [Fact]
    public async Task LockoutEndsAfterDuration_AllowsCorrectPassword()
    {
        using var fx = new SecurityTestFixture(o =>
        {
            o.MaxFailedLoginAttempts = 1;
            o.LockoutDurationMinutes = 5;
        });
        var userId = await fx.RegisterAndActivateAsync("expire@example.com", "expire", "Real-Pass-123!");

        try { await fx.AuthenticationService.LoginAsync("expire@example.com", "Wrong!", null, null); }
        catch (InvalidCredentialsException) { /* expected */ }
        fx.DbContext.ChangeTracker.Clear();

        // Manually expire the lockout (simulating time passage).
        var user = await fx.DbContext.Users.SingleAsync(u => u.Id == userId);
        user.LockoutEnd = DateTime.UtcNow.AddMinutes(-1);
        await fx.DbContext.SaveChangesAsync();
        fx.DbContext.ChangeTracker.Clear();

        var ok = await fx.AuthenticationService.LoginAsync("expire@example.com", "Real-Pass-123!", null, null);
        ok.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("short1!")]              // < 8 chars
    [InlineData("alllowercase1!")]       // missing uppercase
    [InlineData("ALLUPPERCASE1!")]       // missing lowercase
    [InlineData("NoDigitsHere!")]        // missing digit
    [InlineData("NoSpecialChars1")]      // missing special
    [InlineData("password")]             // common weak password (fails most rules)
    public void RegisterValidator_RejectsWeakPasswords(string password)
    {
        var validator = new RegisterDtoValidator();
        var dto = new RegisterDto
        {
            Email = "x@example.com",
            Username = "abc",
            Password = password,
            ConfirmPassword = password
        };

        validator.Validate(dto).Errors.Should()
            .Contain(e => e.PropertyName == nameof(RegisterDto.Password),
                "weak password '{0}' should be rejected by the registration validator", password);
    }

    [Fact]
    public async Task RefreshTokenReuse_RevokesAllUserTokensAndSessions()
    {
        // Replaying a consumed refresh token is treated as compromise: every active token + session
        // for that user must be invalidated to evict the attacker.
        using var fx = new SecurityTestFixture();
        var userId = await fx.RegisterAndActivateAsync("reuse@example.com", "reuse", "Pass123!@#");

        var login = await fx.AuthenticationService.LoginAsync("reuse@example.com", "Pass123!@#", null, null);
        fx.DbContext.ChangeTracker.Clear();
        await fx.AuthenticationService.RefreshTokenAsync(login.RefreshToken, null, null);
        fx.DbContext.ChangeTracker.Clear();

        // Replay the original.
        var act = async () => await fx.AuthenticationService.RefreshTokenAsync(login.RefreshToken, null, null);
        var ex = (await act.Should().ThrowAsync<InvalidRefreshTokenException>()).Which;
        ex.IsReuseDetected.Should().BeTrue();

        // All refresh tokens for this user are either used or revoked.
        var tokens = await fx.DbContext.UserTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.TokenType == DainnUser.Core.Enums.TokenType.RefreshToken)
            .ToListAsync();
        tokens.Should().NotBeEmpty();
        tokens.Should().OnlyContain(t => t.IsRevoked || t.IsUsed);

        // All sessions deactivated.
        var sessions = await fx.DbContext.UserSessions.AsNoTracking()
            .Where(s => s.UserId == userId).ToListAsync();
        sessions.Should().OnlyContain(s => !s.IsActive);
    }

    [Fact]
    public async Task UnverifiedEmail_BlocksLogin_WhenVerificationRequired()
    {
        // When email verification is required, login must NOT succeed even with the correct
        // password — proving email ownership is a precondition.
        using var fx = new SecurityTestFixture(o => o.RequireEmailVerification = true);
        await fx.AuthenticationService.RegisterAsync("unverified@example.com", "unverified", "Pass123!@#");
        // Note: not calling RegisterAndActivateAsync — user stays unverified.

        var act = async () => await fx.AuthenticationService.LoginAsync("unverified@example.com", "Pass123!@#", null, null);

        await act.Should().ThrowAsync<EmailNotVerifiedException>();
    }

    [Fact]
    public async Task RevokedRefreshToken_IsRejected()
    {
        using var fx = new SecurityTestFixture();
        var userId = await fx.RegisterAndActivateAsync("rev@example.com", "rev", "Pass123!@#");
        var login = await fx.AuthenticationService.LoginAsync("rev@example.com", "Pass123!@#", null, null);
        fx.DbContext.ChangeTracker.Clear();

        // Manually revoke the token.
        await fx.UserRepository.RevokeAllRefreshTokensAsync(userId);
        await fx.DbContext.SaveChangesAsync();
        fx.DbContext.ChangeTracker.Clear();

        var act = async () => await fx.AuthenticationService.RefreshTokenAsync(login.RefreshToken, null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }

    [Fact]
    public async Task SuccessfulLogin_ResetsFailedLoginCounter()
    {
        // Once authentication succeeds, failed-attempt counters must reset so a legitimate user
        // doesn't carry stale state into a future lockout.
        using var fx = new SecurityTestFixture(o => o.MaxFailedLoginAttempts = 5);
        var userId = await fx.RegisterAndActivateAsync("reset@example.com", "reset", "Real-Pass-123!");

        for (var i = 0; i < 3; i++)
        {
            try { await fx.AuthenticationService.LoginAsync("reset@example.com", "Wrong!", null, null); }
            catch (InvalidCredentialsException) { /* expected */ }
            fx.DbContext.ChangeTracker.Clear();
        }

        await fx.AuthenticationService.LoginAsync("reset@example.com", "Real-Pass-123!", null, null);

        var user = await fx.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == userId);
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
    }

    [Fact]
    public async Task NewLockout_NotifiesAccountOwnerExactlyOnce()
    {
        // The owner must hear about a lockout (it might be theft). But repeat brute-force
        // attempts during the same active lockout must NOT spam them with duplicate emails.
        using var fx = new SecurityTestFixture(o => o.MaxFailedLoginAttempts = 2);
        await fx.RegisterAndActivateAsync("alert@example.com", "alert", "Real-Pass-1!");

        for (var i = 0; i < 3; i++)
        {
            try { await fx.AuthenticationService.LoginAsync("alert@example.com", "Wrong-Pass-1!", null, null); }
            catch (InvalidCredentialsException) { /* expected */ }
            catch (AccountLockedException) { /* expected once locked */ }
            fx.DbContext.ChangeTracker.Clear();
        }

        fx.EmailServiceMock.Verify(e => e.SendAccountLockoutNotificationAsync(
            "alert@example.com",
            "alert",
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()),
            Times.Once,
            "lockout email should fire on the lock-triggering attempt only, not on subsequent rejected attempts");
    }

    [Fact]
    public async Task AdminUnlock_RestoresLogin_AfterBruteForceLockout()
    {
        // After admin unlock, the legitimate password must work again on the next attempt —
        // verifies the unlock end-to-end (counters cleared, status restored if needed).
        using var fx = new SecurityTestFixture(o => o.MaxFailedLoginAttempts = 2);
        var userId = await fx.RegisterAndActivateAsync("admin-unlock@example.com", "adminunlock", "Real-Pass-1!");

        for (var i = 0; i < 2; i++)
        {
            try { await fx.AuthenticationService.LoginAsync("admin-unlock@example.com", "Wrong!", null, null); }
            catch (InvalidCredentialsException) { }
            fx.DbContext.ChangeTracker.Clear();
        }

        // Confirm locked.
        var stillLockedAct = async () => await fx.AuthenticationService.LoginAsync("admin-unlock@example.com", "Real-Pass-1!", null, null);
        await stillLockedAct.Should().ThrowAsync<AccountLockedException>();
        fx.DbContext.ChangeTracker.Clear();

        // Admin unlocks.
        (await fx.AuthenticationService.UnlockAccountAsync(userId)).Should().BeTrue();

        // Login now succeeds.
        var login = await fx.AuthenticationService.LoginAsync("admin-unlock@example.com", "Real-Pass-1!", null, null);
        login.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoginAttempts_AreRecordedInLoginHistory_BothSuccessAndFailure()
    {
        // Audit trail for OWASP A09 (logging failures): every authentication attempt — success
        // or failure — leaves a row.
        using var fx = new SecurityTestFixture();
        var userId = await fx.RegisterAndActivateAsync("audit@example.com", "audit", "Pass123!@#");

        try { await fx.AuthenticationService.LoginAsync("audit@example.com", "wrong", null, null); }
        catch (InvalidCredentialsException) { }
        fx.DbContext.ChangeTracker.Clear();
        await fx.AuthenticationService.LoginAsync("audit@example.com", "Pass123!@#", "1.2.3.4", "ua");

        var history = await fx.DbContext.LoginHistories.AsNoTracking()
            .Where(h => h.UserId == userId).ToListAsync();
        history.Should().HaveCount(2);
        history.Should().Contain(h => !h.IsSuccessful);
        history.Should().Contain(h => h.IsSuccessful && h.IpAddress == "1.2.3.4" && h.UserAgent == "ua");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // PSA-62 — Password Reset Security
    // ──────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ForgotPassword_UnknownEmail_DoesNotRevealUserExistence()
    {
        // OWASP A07: password-reset endpoint must not leak whether an email is registered.
        using var fx = new SecurityTestFixture();

        // ForgotPasswordAsync should complete silently — no exception, no email sent.
        var act = async () => await fx.AuthenticationService.ForgotPasswordAsync("ghost@example.com");
        await act.Should().NotThrowAsync();

        fx.EmailServiceMock.Verify(
            x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ForgotPassword_ResetTokenStoredAsHash_PlainTextNotInDatabase()
    {
        using var fx = new SecurityTestFixture();
        var userId = await fx.RegisterAndActivateAsync("user@example.com", "user", "Pass-123!");

        string? capturedPlainToken = null;
        fx.EmailServiceMock
            .Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, token, _) => capturedPlainToken = token)
            .Returns(Task.CompletedTask);

        await fx.AuthenticationService.ForgotPasswordAsync("user@example.com");
        fx.DbContext.ChangeTracker.Clear();

        capturedPlainToken.Should().NotBeNullOrEmpty("email should have been sent");

        // The plain token must NOT be stored verbatim anywhere in the token table.
        var storedTokens = await fx.DbContext.Set<DainnUser.Core.Entities.UserToken>()
            .Where(t => t.UserId == userId && t.TokenType == DainnUser.Core.Enums.TokenType.PasswordReset)
            .ToListAsync();

        storedTokens.Should().HaveCount(1);
        storedTokens[0].TokenValue.Should().NotBe(capturedPlainToken, "only the hash may be stored");
    }

    [Fact]
    public async Task ResetPassword_ExpiredToken_Rejected()
    {
        using var fx = new SecurityTestFixture();
        var userId = await fx.RegisterAndActivateAsync("expired@example.com", "expired", "Pass-123!");

        // Manually insert a token that's already expired.
        var plainToken = "my-plain-reset-token";
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(plainToken))).ToLowerInvariant();

        await fx.DbContext.Set<DainnUser.Core.Entities.UserToken>().AddAsync(new DainnUser.Core.Entities.UserToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenType = DainnUser.Core.Enums.TokenType.PasswordReset,
            TokenValue = hash,
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // expired
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        });
        await fx.DbContext.SaveChangesAsync();
        fx.DbContext.ChangeTracker.Clear();

        var act = async () => await fx.AuthenticationService.ResetPasswordAsync(plainToken, "New-Pass-123!");
        await act.Should().ThrowAsync<DainnUser.Core.Exceptions.InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ResetPassword_TokenCannotBeUsedTwice()
    {
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("reuse@example.com", "reuse", "Pass-123!");

        string capturedToken = string.Empty;
        fx.EmailServiceMock
            .Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, t, _) => capturedToken = t)
            .Returns(Task.CompletedTask);
        fx.EmailServiceMock
            .Setup(x => x.SendPasswordChangedNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await fx.AuthenticationService.ForgotPasswordAsync("reuse@example.com");
        fx.DbContext.ChangeTracker.Clear();

        // First use succeeds.
        await fx.AuthenticationService.ResetPasswordAsync(capturedToken, "New-Pass-1!");
        fx.DbContext.ChangeTracker.Clear();

        // Second use with the same token must fail.
        var act = async () => await fx.AuthenticationService.ResetPasswordAsync(capturedToken, "Another-Pass-2!");
        await act.Should().ThrowAsync<DainnUser.Core.Exceptions.InvalidPasswordResetTokenException>();
    }

    [Fact]
    public async Task ResetPassword_RevokesAllRefreshTokensAndSessions()
    {
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("session@example.com", "session", "Pass-123!");

        // Log in to create refresh token + session.
        fx.EmailServiceMock.Setup(x => x.SendPasswordChangedNotificationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var loginResult = await fx.AuthenticationService.LoginAsync("session@example.com", "Pass-123!", null, null);
        fx.DbContext.ChangeTracker.Clear();

        // Initiate reset.
        string resetToken = string.Empty;
        fx.EmailServiceMock
            .Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((_, _, t, _) => resetToken = t)
            .Returns(Task.CompletedTask);

        await fx.AuthenticationService.ForgotPasswordAsync("session@example.com");
        fx.DbContext.ChangeTracker.Clear();

        await fx.AuthenticationService.ResetPasswordAsync(resetToken, "Brand-New-Pass-1!");
        fx.DbContext.ChangeTracker.Clear();

        // All refresh tokens are revoked.
        var refreshTokens = await fx.DbContext.Set<DainnUser.Core.Entities.UserToken>()
            .Where(t => t.TokenType == DainnUser.Core.Enums.TokenType.RefreshToken)
            .ToListAsync();
        refreshTokens.Should().AllSatisfy(t => t.IsRevoked.Should().BeTrue());

        // Session is deactivated.
        var sessions = await fx.DbContext.Set<DainnUser.Core.Entities.UserSession>()
            .Where(s => s.Id == loginResult.SessionId)
            .ToListAsync();
        sessions.Should().AllSatisfy(s => s.IsActive.Should().BeFalse());
    }
}
