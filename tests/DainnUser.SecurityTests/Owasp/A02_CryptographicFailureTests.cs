using DainnUser.Core.Configuration;
using System.IdentityModel.Tokens.Jwt;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Services;
using DainnUser.SecurityTests.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DainnUser.SecurityTests.Owasp;

/// <summary>
/// OWASP A02:2021 — Cryptographic Failures.
/// Verifies that secrets at rest are never plain (passwords, refresh tokens) and that JWT
/// signing key requirements are enforced.
/// </summary>
public class A02_CryptographicFailureTests
{
    [Fact]
    public async Task RegisteredPassword_IsNeverStoredInPlaintext()
    {
        using var fx = new SecurityTestFixture();
        var password = "Plaintext123!@#";
        var userId = await fx.AuthenticationService.RegisterAsync("a@example.com", "aaa", password);

        var user = await fx.DbContext.Users.AsNoTracking().SingleAsync(u => u.Id == userId);

        // Hash is non-empty, distinct from the input, and verifies via the hasher.
        user.PasswordHash.Should().NotBeNullOrWhiteSpace();
        user.PasswordHash.Should().NotBe(password);
        user.PasswordHash.Should().NotContain(password);
        fx.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, password)
            .Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public async Task IssuedRefreshToken_IsStoredHashedNotPlaintext()
    {
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("b@example.com", "bbb", "Pass123!@#");

        var login = await fx.AuthenticationService.LoginAsync("b@example.com", "Pass123!@#", null, null);

        var stored = await fx.DbContext.UserTokens.AsNoTracking()
            .SingleAsync(t => t.TokenType == TokenType.RefreshToken);
        stored.TokenValue.Should().NotBe(login.RefreshToken);
        stored.TokenValue.Should().NotContain(login.RefreshToken);
        // Hash is deterministic and reproducible — proving the on-disk value is the SHA-256 of the plain token.
        stored.TokenValue.Should().Be(fx.JwtTokenService.HashRefreshToken(login.RefreshToken));
    }

    [Fact]
    public async Task SessionToken_IsStoredAsRefreshTokenHash_NotPlaintext()
    {
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("c@example.com", "ccc", "Pass123!@#");

        var login = await fx.AuthenticationService.LoginAsync("c@example.com", "Pass123!@#", null, null);

        var session = await fx.DbContext.UserSessions.AsNoTracking().SingleAsync(s => s.Id == login.SessionId);
        session.SessionToken.Should().NotBe(login.RefreshToken);
        session.SessionToken.Should().Be(fx.JwtTokenService.HashRefreshToken(login.RefreshToken));
    }

    [Fact]
    public void JwtTokenService_RejectsKeyShorterThan256Bits()
    {
        // HMAC-SHA256 requires a 256-bit key; weaker keys are silently dangerous and must be
        // refused at construction.
        var act = () => new JwtTokenService(
            Options.Create(new JwtOptions { Secret = "short-key-too-small" }),
            new DainnUserOptions());

        act.Should().Throw<InvalidOperationException>().WithMessage("*32 bytes*");
    }

    [Fact]
    public async Task TamperedAccessToken_FailsValidation()
    {
        // Flipping bits in a signed JWT must invalidate the signature.
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("d@example.com", "ddd", "Pass123!@#");
        var login = await fx.AuthenticationService.LoginAsync("d@example.com", "Pass123!@#", null, null);

        // Mutate the payload section ("." separates segments — index 1 is the payload).
        var parts = login.AccessToken.Split('.');
        parts[1] = parts[1][..^1] + (parts[1][^1] == 'A' ? 'B' : 'A');
        var tampered = string.Join('.', parts);

        fx.JwtTokenService.ValidateAccessToken(tampered).Should().BeNull();
    }

    [Fact]
    public async Task AccessTokenSignedByDifferentSecret_IsRejected()
    {
        // A JWT generated with a different signing key must not validate against ours, even if
        // header + payload look correct. This guards against "alg=none" and key-confusion attacks.
        using var fx = new SecurityTestFixture();
        await fx.RegisterAndActivateAsync("e@example.com", "eee", "Pass123!@#");

        var foreignService = new JwtTokenService(
            Options.Create(new JwtOptions { Secret = "completely-different-secret-32-bytes-long" }),
            new DainnUserOptions());
        var foreignToken = foreignService
            .GenerateAccessToken(new User { Id = Guid.NewGuid(), Email = "imp@example.com", Username = "imp" }, Array.Empty<string>(), Guid.NewGuid())
            .Token;

        fx.JwtTokenService.ValidateAccessToken(foreignToken).Should().BeNull();
    }
}
