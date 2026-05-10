using DainnUser.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DainnUser.SecurityTests.Owasp;

/// <summary>
/// OWASP A05:2021 — Security Misconfiguration.
/// Startup must fail fast when required security configuration is missing or weak; the library
/// must not silently fall back to insecure defaults.
/// </summary>
public class A05_SecurityMisconfigurationTests
{
    private static IConfiguration BuildConfig(Action<Dictionary<string, string?>>? mutate = null)
    {
        var dict = new Dictionary<string, string?>
        {
            ["DainnUser:Database:Provider"] = "SQLite",
            ["DainnUser:Database:ConnectionString"] = "Data Source=test.db",
            ["DainnUser:Email:SmtpHost"] = "localhost",
            ["DainnUser:Email:SmtpPort"] = "1025",
            ["DainnUser:Email:FromEmail"] = "noreply@example.com",
            ["DainnUser:Jwt:Secret"] = "test-jwt-secret-must-be-at-least-32-bytes-okay"
        };
        mutate?.Invoke(dict);
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    [Fact]
    public void AddDainnUser_FailsFast_WhenJwtSecretIsMissing()
    {
        var config = BuildConfig(d => d.Remove("DainnUser:Jwt:Secret"));
        var act = () => new ServiceCollection().AddDainnUser(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT secret*");
    }

    [Fact]
    public void AddDainnUser_FailsFast_WhenJwtSecretIsTooShort()
    {
        // < 32 bytes is unsafe for HMAC-SHA256 and must be rejected at startup, not at first request.
        var config = BuildConfig(d => d["DainnUser:Jwt:Secret"] = "weak-secret-only-22b!");
        var act = () => new ServiceCollection().AddDainnUser(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*too short*");
    }

    [Fact]
    public void AddDainnUser_FailsFast_WhenDatabaseConnectionStringIsMissing()
    {
        var config = BuildConfig(d => d.Remove("DainnUser:Database:ConnectionString"));
        var act = () => new ServiceCollection().AddDainnUser(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*connection string*");
    }

    [Fact]
    public void AddDainnUser_FailsFast_WhenDatabaseProviderIsUnknown()
    {
        // Silent fallback to a default provider would hide misconfiguration; reject explicitly.
        var config = BuildConfig(d => d["DainnUser:Database:Provider"] = "Oracle");
        var act = () => new ServiceCollection().AddDainnUser(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Unsupported*");
    }

    [Fact]
    public void AddDainnUser_FailsFast_WhenEmailFromAddressMissing()
    {
        var config = BuildConfig(d => d.Remove("DainnUser:Email:FromEmail"));
        var act = () => new ServiceCollection().AddDainnUser(config);

        act.Should().Throw<InvalidOperationException>().WithMessage("*from address*");
    }

    [Fact]
    public void RateLimiterRegistry_FailsFast_WhenRuleHasNonPositiveLimit()
    {
        // Misconfigured rules (zero or negative permits) are caught at startup so they don't
        // accidentally produce a permissive limiter (PermitLimit=0 would deny everything; <0 is
        // undefined behavior in the BCL).
        var config = BuildConfig(d =>
        {
            d["DainnUser:RateLimiting:Rules:0:Endpoint"] = "/api/auth/login";
            d["DainnUser:RateLimiting:Rules:0:MaxRequests"] = "0";
            d["DainnUser:RateLimiting:Rules:0:WindowSeconds"] = "60";
        });
        var services = new ServiceCollection();
        services.AddDainnUser(config);
        var provider = services.BuildServiceProvider();

        // RateLimiterRegistry is constructed lazily; resolving it should throw the validation error.
        var act = () => provider.GetRequiredService<DainnUser.Infrastructure.Middleware.RateLimiterRegistry>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxRequests*");
    }
}
