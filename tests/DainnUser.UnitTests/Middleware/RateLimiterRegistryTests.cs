using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Middleware;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace DainnUser.UnitTests.Middleware;

public class RateLimiterRegistryTests
{
    private static RateLimiterRegistry Build(RateLimitingOptions options)
        => new(Options.Create(options));

    [Fact]
    public void IsEnabled_FalseWhenDisabled()
    {
        var registry = Build(new RateLimitingOptions
        {
            Enabled = false,
            Rules = { new RateLimitRule { Endpoint = "/x", MaxRequests = 1, WindowSeconds = 1 } }
        });

        registry.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_FalseWhenNoRules()
    {
        var registry = Build(new RateLimitingOptions { Enabled = true });
        registry.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Constructor_RejectsEmptyEndpoint()
    {
        var act = () => Build(new RateLimitingOptions
        {
            Rules = { new RateLimitRule { Endpoint = "", MaxRequests = 5, WindowSeconds = 60 } }
        });
        act.Should().Throw<InvalidOperationException>().WithMessage("*Endpoint*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveMaxRequests(int maxRequests)
    {
        var act = () => Build(new RateLimitingOptions
        {
            Rules = { new RateLimitRule { Endpoint = "/x", MaxRequests = maxRequests, WindowSeconds = 60 } }
        });
        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxRequests*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_RejectsNonPositiveWindow(int window)
    {
        var act = () => Build(new RateLimitingOptions
        {
            Rules = { new RateLimitRule { Endpoint = "/x", MaxRequests = 5, WindowSeconds = window } }
        });
        act.Should().Throw<InvalidOperationException>().WithMessage("*WindowSeconds*");
    }

    [Fact]
    public void Resolve_ReturnsExactMatchOverPrefix()
    {
        // Exact matches must win even if a prefix rule is listed first.
        var registry = Build(new RateLimitingOptions
        {
            Rules =
            {
                new RateLimitRule { Endpoint = "/api/auth/login", MaxRequests = 5, WindowSeconds = 60 },
                new RateLimitRule { Endpoint = "/api/auth/*", MaxRequests = 30, WindowSeconds = 60 }
            }
        });

        var rule = registry.Resolve("/api/auth/login");
        rule.Should().NotBeNull();
        rule!.Rule.MaxRequests.Should().Be(5);
    }

    [Fact]
    public void Resolve_FallsBackToPrefix()
    {
        var registry = Build(new RateLimitingOptions
        {
            Rules =
            {
                new RateLimitRule { Endpoint = "/api/auth/login", MaxRequests = 5, WindowSeconds = 60 },
                new RateLimitRule { Endpoint = "/api/auth/*", MaxRequests = 30, WindowSeconds = 60 }
            }
        });

        var rule = registry.Resolve("/api/auth/refresh");
        rule.Should().NotBeNull();
        rule!.Rule.MaxRequests.Should().Be(30);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive()
    {
        var registry = Build(new RateLimitingOptions
        {
            Rules = { new RateLimitRule { Endpoint = "/api/auth/login", MaxRequests = 5, WindowSeconds = 60 } }
        });

        registry.Resolve("/API/Auth/Login").Should().NotBeNull();
    }

    [Fact]
    public void Resolve_ReturnsNullForUnmatchedPath()
    {
        var registry = Build(new RateLimitingOptions
        {
            Rules = { new RateLimitRule { Endpoint = "/api/auth/login", MaxRequests = 5, WindowSeconds = 60 } }
        });

        registry.Resolve("/api/profile").Should().BeNull();
    }

    [Fact]
    public void IsWhitelisted_HonorsConfiguredIps()
    {
        var registry = Build(new RateLimitingOptions
        {
            Rules = { new RateLimitRule { Endpoint = "/x", MaxRequests = 1, WindowSeconds = 1 } },
            WhitelistIps = { "127.0.0.1", "10.0.0.5" }
        });

        registry.IsWhitelisted("127.0.0.1").Should().BeTrue();
        registry.IsWhitelisted("10.0.0.5").Should().BeTrue();
        registry.IsWhitelisted("8.8.8.8").Should().BeFalse();
        registry.IsWhitelisted(null).Should().BeFalse();
        registry.IsWhitelisted("").Should().BeFalse();
    }
}
