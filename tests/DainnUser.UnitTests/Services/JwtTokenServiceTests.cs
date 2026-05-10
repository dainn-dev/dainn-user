using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DainnUser.Core.Authorization;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DainnUser.UnitTests.Services;

public class JwtTokenServiceTests
{
    private const string Secret = "test-secret-must-be-at-least-32-bytes-long-okay-yes";

    private static JwtTokenService CreateService(
        DainnUserOptions? options = null,
        JwtOptions? jwtOptions = null)
    {
        return new JwtTokenService(
            Options.Create(jwtOptions ?? new JwtOptions { Secret = Secret }),
            options ?? new DainnUserOptions());
    }

    private static User SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "user@example.com",
        Username = "user",
        EmailVerified = true,
        Status = UserStatus.Active
    };

    [Fact]
    public void Constructor_WithMissingSecret_Throws()
    {
        var act = () => new JwtTokenService(
            Options.Create(new JwtOptions { Secret = "" }),
            new DainnUserOptions());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithShortSecret_Throws()
    {
        var act = () => new JwtTokenService(
            Options.Create(new JwtOptions { Secret = "too-short" }),
            new DainnUserOptions());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GenerateAccessToken_IncludesUserClaimsAndSessionId()
    {
        var service = CreateService();
        var user = SampleUser();
        var sessionId = Guid.NewGuid();

        var result = service.GenerateAccessToken(user, new[] { "Admin", "User" }, sessionId);

        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
        jwt.Claims.Should().Contain(c => c.Type == "sid" && c.Value == sessionId.ToString());
        jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Should().BeEquivalentTo(new[] { "Admin", "User" });
    }

    [Fact]
    public void GenerateAccessToken_WithPermissions_IncludesPermissionClaims()
    {
        var service = CreateService();
        var user = SampleUser();

        var result = service.GenerateAccessToken(
            user,
            new[] { "Admin" },
            new[] { "Users:Delete", "users:delete", " users:read " },
            Guid.NewGuid());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);
        jwt.Claims
            .Where(c => c.Type == DainnUserClaimTypes.Permission)
            .Select(c => c.Value)
            .Should().BeEquivalentTo(new[] { "users:delete", "users:read" });
    }

    [Fact]
    public void GenerateAccessToken_ExpirationMatchesOptions()
    {
        var service = CreateService(new DainnUserOptions { JwtExpirationMinutes = 5 });
        var result = service.GenerateAccessToken(SampleUser(), Array.Empty<string>(), Guid.NewGuid());

        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GenerateRefreshToken_ProducesDistinctValues()
    {
        var service = CreateService();
        var first = service.GenerateRefreshToken();
        var second = service.GenerateRefreshToken();

        first.Should().NotBeNullOrWhiteSpace();
        first.Should().NotBe(second);
        // URL-safe base64 — must not contain '+' or '/'
        first.Should().NotContain("+").And.NotContain("/");
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic_AndDiffersFromInput()
    {
        var service = CreateService();
        var token = service.GenerateRefreshToken();
        var h1 = service.HashRefreshToken(token);
        var h2 = service.HashRefreshToken(token);

        h1.Should().Be(h2);
        h1.Should().NotBe(token);
    }

    [Fact]
    public void ValidateAccessToken_RoundTripsValidToken()
    {
        var service = CreateService();
        var user = SampleUser();
        var token = service.GenerateAccessToken(user, new[] { "User" }, Guid.NewGuid()).Token;

        var principal = service.ValidateAccessToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirst(JwtRegisteredClaimNames.Sub)?.Value.Should().Be(user.Id.ToString());
    }

    [Fact]
    public void ValidateAccessToken_WithTamperedToken_ReturnsNull()
    {
        var service = CreateService();
        var token = service.GenerateAccessToken(SampleUser(), Array.Empty<string>(), Guid.NewGuid()).Token;
        // Flip one character in the signature segment to invalidate the HMAC.
        var tampered = token[..^2] + (token[^1] == 'a' ? "bb" : "aa");

        var principal = service.ValidateAccessToken(tampered);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateAccessToken_WithDifferentSecret_ReturnsNull()
    {
        var issuer = CreateService();
        var verifier = CreateService(jwtOptions: new JwtOptions { Secret = "different-secret-also-32-bytes-long-yes!" });
        var token = issuer.GenerateAccessToken(SampleUser(), Array.Empty<string>(), Guid.NewGuid()).Token;

        verifier.ValidateAccessToken(token).Should().BeNull();
    }
}
