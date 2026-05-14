using DainnUser.Core.Configuration;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;

namespace DainnUser.SecurityTests.Owasp;

/// <summary>
/// OWASP A04:2021 — Insecure Design.
/// Verifies that defaults are secure: features that protect users (lockout, email verification,
/// rate limiting) must be ON unless explicitly disabled, not the other way around.
/// </summary>
public class A04_InsecureDesignTests
{
    [Fact]
    public void DainnUserOptions_DefaultsHaveLockoutEnabled()
    {
        var options = new DainnUserOptions();

        options.EnableAccountLockout.Should().BeTrue("brute-force protection must be on by default");
        options.MaxFailedLoginAttempts.Should().BeLessThanOrEqualTo(10, "high attempt limits defeat the purpose of lockout");
        options.LockoutDurationMinutes.Should().BeGreaterThan(0);
    }

    [Fact]
    public void DainnUserOptions_DefaultsRequireEmailVerification()
    {
        var options = new DainnUserOptions();

        options.RequireEmailVerification.Should().BeTrue(
            "registration must not auto-grant access — email ownership should be proven first");
    }

    [Fact]
    public void DainnUserOptions_DefaultsHaveRateLimitingEnabled()
    {
        var options = new DainnUserOptions();

        options.EnableRateLimiting.Should().BeTrue("rate limiting must be on by default");
    }

    [Fact]
    public void DainnUserOptions_DefaultsDoNotEnableRiskyFeaturesUnsolicited()
    {
        var options = new DainnUserOptions();

        // Social login and 2FA are off by default — turning them on requires consumer opt-in
        // (and configuring the relevant provider secrets), so they can't be partially configured
        // and exposed accidentally.
        options.EnableSocialLogin.Should().BeFalse();
        options.EnableTwoFactor.Should().BeFalse();
    }

    [Fact]
    public void DainnUserOptions_RefreshTokenLifetimeIsBounded()
    {
        var options = new DainnUserOptions();

        // Default refresh token lifetime should be reasonable — long-lived tokens enlarge the
        // theft window. 7 days is the documented default; assertion gates regressions.
        options.RefreshTokenExpirationDays.Should().BeInRange(1, 30);
    }

    [Fact]
    public void RateLimitingOptions_DefaultsAreEnabledButHaveNoRules()
    {
        // Empty rules + Enabled=true means the middleware is a no-op until consumers add rules,
        // which is safer than auto-rate-limiting unknown endpoints.
        var options = new RateLimitingOptions();

        options.Enabled.Should().BeTrue();
        options.Rules.Should().BeEmpty("consumers must opt in per-endpoint to avoid accidental DoS of legitimate routes");
        options.WhitelistIps.Should().BeEmpty("no IP gets free passes by default");
    }

    [Fact]
    public void JwtOptions_DefaultsValidateIssuerAndAudience()
    {
        var options = new JwtOptions();

        options.ValidateIssuer.Should().BeTrue();
        options.ValidateAudience.Should().BeTrue();
    }
}
