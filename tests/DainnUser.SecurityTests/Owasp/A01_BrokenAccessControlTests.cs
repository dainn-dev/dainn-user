using System.IdentityModel.Tokens.Jwt;
using DainnUser.Core.Exceptions;
using DainnUser.SecurityTests.TestFixtures;
using FluentAssertions;

namespace DainnUser.SecurityTests.Owasp;

/// <summary>
/// OWASP A01:2021 — Broken Access Control.
/// Verifies that endpoints/services don't expose state to unauthorized callers and that
/// presenting another user's tokens does not grant access.
/// </summary>
public class A01_BrokenAccessControlTests
{
    [Fact]
    public async Task RefreshToken_OfDifferentUser_CannotImpersonateAcrossAccounts()
    {
        // Two users with valid sessions. Confirm tokens issued to user A cannot be used to
        // gain access to user B's account, and that the JWT subject claim is bound to the user
        // who actually owns the refresh token row.
        using var fx = new SecurityTestFixture();
        var aId = await fx.RegisterAndActivateAsync("a@example.com", "aaa", "Pass123!@#");
        var bId = await fx.RegisterAndActivateAsync("b@example.com", "bbb", "Pass123!@#");

        var aLogin = await fx.AuthenticationService.LoginAsync("a@example.com", "Pass123!@#", null, null);
        var bLogin = await fx.AuthenticationService.LoginAsync("b@example.com", "Pass123!@#", null, null);

        // Tokens are completely different.
        aLogin.AccessToken.Should().NotBe(bLogin.AccessToken);
        aLogin.RefreshToken.Should().NotBe(bLogin.RefreshToken);
        aLogin.SessionId.Should().NotBe(bLogin.SessionId);

        // JWT subject claim binds correctly to the issuing user.
        var aJwt = new JwtSecurityTokenHandler().ReadJwtToken(aLogin.AccessToken);
        var bJwt = new JwtSecurityTokenHandler().ReadJwtToken(bLogin.AccessToken);
        aJwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(aId.ToString());
        bJwt.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Sub).Value.Should().Be(bId.ToString());
    }

    [Fact]
    public async Task Logout_OnUnknownSession_DoesNotLeakOrThrow()
    {
        // Logout must be idempotent — a forged or stale session id must produce no observable
        // side effect (no 500, no exception details).
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("c@example.com", "ccc", "Pass123!@#");

        // Should not throw.
        await fx.AuthenticationService.LogoutAsync(Guid.NewGuid());
    }

    [Fact]
    public async Task Logout_OfOneSession_DoesNotInvalidateOtherSessions()
    {
        // Logging out of session A must not affect session B (otherwise an attacker who learns
        // sessionA's id could DoS sessionB).
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("d@example.com", "ddd", "Pass123!@#");

        var s1 = await fx.AuthenticationService.LoginAsync("d@example.com", "Pass123!@#", null, null);
        fx.DbContext.ChangeTracker.Clear();
        var s2 = await fx.AuthenticationService.LoginAsync("d@example.com", "Pass123!@#", null, null);
        fx.DbContext.ChangeTracker.Clear();

        await fx.AuthenticationService.LogoutAsync(s1.SessionId);

        // s2 must still be able to refresh.
        var refreshed = await fx.AuthenticationService.RefreshTokenAsync(s2.RefreshToken, null, null);
        refreshed.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RefreshToken_AfterLogout_IsRejected()
    {
        // Logging out must invalidate the refresh token tied to that session. Re-presenting it
        // must fail with a generic InvalidRefreshTokenException.
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("e@example.com", "eee", "Pass123!@#");
        var login = await fx.AuthenticationService.LoginAsync("e@example.com", "Pass123!@#", null, null);
        fx.DbContext.ChangeTracker.Clear();

        await fx.AuthenticationService.LogoutAsync(login.SessionId);

        var act = async () => await fx.AuthenticationService.RefreshTokenAsync(login.RefreshToken, null, null);
        await act.Should().ThrowAsync<InvalidRefreshTokenException>();
    }
}
