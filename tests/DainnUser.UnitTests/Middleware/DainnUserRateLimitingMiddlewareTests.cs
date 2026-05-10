using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DainnUser.UnitTests.Middleware;

public class DainnUserRateLimitingMiddlewareTests
{
    private const string Path = "/api/auth/login";

    private static (DainnUserRateLimitingMiddleware mw, NextSpy next) Build(
        RateLimitingOptions rateOptions,
        DainnUserOptions? dainnOptions = null)
    {
        var registry = new RateLimiterRegistry(Options.Create(rateOptions));
        var next = new NextSpy();
        var mw = new DainnUserRateLimitingMiddleware(
            next.InvokeAsync,
            registry,
            dainnOptions ?? new DainnUserOptions { EnableRateLimiting = true },
            NullLogger<DainnUserRateLimitingMiddleware>.Instance);
        return (mw, next);
    }

    private static HttpContext NewContext(
        string path = Path,
        string? remoteIp = "10.0.0.1",
        string? forwardedFor = null,
        ClaimsPrincipal? user = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        if (remoteIp is not null)
        {
            ctx.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        }
        if (forwardedFor is not null)
        {
            ctx.Request.Headers["X-Forwarded-For"] = forwardedFor;
        }
        if (user is not null)
        {
            ctx.User = user;
        }
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private sealed class NextSpy
    {
        public int CallCount { get; private set; }
        public Task InvokeAsync(HttpContext _)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private static RateLimitingOptions LoginRule(int max, int window = 60)
        => new()
        {
            Enabled = true,
            Rules = { new RateLimitRule { Endpoint = Path, MaxRequests = max, WindowSeconds = window, SegmentsPerWindow = 6 } }
        };

    [Fact]
    public async Task PassesThrough_WhenDainnUserOptionsDisableRateLimiting()
    {
        var (mw, next) = Build(LoginRule(1), new DainnUserOptions { EnableRateLimiting = false });
        await mw.InvokeAsync(NewContext());
        await mw.InvokeAsync(NewContext());

        next.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task PassesThrough_WhenRateLimitingOptionsDisabled()
    {
        var options = LoginRule(1);
        options.Enabled = false;
        var (mw, next) = Build(options);

        await mw.InvokeAsync(NewContext());
        await mw.InvokeAsync(NewContext());

        next.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task PassesThrough_WhenPathDoesNotMatch()
    {
        var (mw, next) = Build(LoginRule(1));
        var ctx = NewContext(path: "/api/profile");

        await mw.InvokeAsync(ctx);

        next.CallCount.Should().Be(1);
        ctx.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task AllowsUntilLimit_Then429sWithRetryAfter()
    {
        var (mw, next) = Build(LoginRule(2));

        await mw.InvokeAsync(NewContext());
        await mw.InvokeAsync(NewContext());
        var blocked = NewContext();
        await mw.InvokeAsync(blocked);

        next.CallCount.Should().Be(2);
        blocked.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        blocked.Response.Headers.ContainsKey("Retry-After").Should().BeTrue();
        int.Parse(blocked.Response.Headers["Retry-After"].ToString()).Should().BeGreaterThan(0);

        blocked.Response.Body.Position = 0;
        var body = await new StreamReader(blocked.Response.Body).ReadToEndAsync();
        body.Should().Contain("Too many requests");
    }

    [Fact]
    public async Task SeparateIps_HaveSeparateBuckets()
    {
        var (mw, next) = Build(LoginRule(1));

        await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.1"));
        await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.2"));

        next.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task WhitelistedIp_BypassesLimit()
    {
        var options = LoginRule(1);
        options.WhitelistIps.Add("10.0.0.1");
        var (mw, next) = Build(options);

        for (var i = 0; i < 5; i++)
        {
            await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.1"));
        }

        next.CallCount.Should().Be(5);
    }

    [Fact]
    public async Task XForwardedFor_TakesPrecedenceOverConnectionIp()
    {
        var (mw, next) = Build(LoginRule(1));

        // Two requests with different XFF — same connection IP — should each get their own bucket.
        await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.99", forwardedFor: "1.1.1.1"));
        await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.99", forwardedFor: "2.2.2.2"));

        next.CallCount.Should().Be(2);
    }

    [Fact]
    public async Task PerUserMode_PartitionsByUserId()
    {
        var options = LoginRule(1);
        options.Rules[0].Mode = RateLimitMode.PerUser;
        var (mw, next) = Build(options);

        var u1 = MakeAuthenticatedUser("user-1");
        var u2 = MakeAuthenticatedUser("user-2");

        // Same IP, different users → 2 separate buckets.
        await mw.InvokeAsync(NewContext(user: u1));
        await mw.InvokeAsync(NewContext(user: u2));

        // Same user again → blocked.
        var blocked = NewContext(user: u1);
        await mw.InvokeAsync(blocked);

        next.CallCount.Should().Be(2);
        blocked.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task PerUserMode_FallsBackToIpForAnonymous()
    {
        var options = LoginRule(1);
        options.Rules[0].Mode = RateLimitMode.PerUser;
        var (mw, next) = Build(options);

        // Same IP, no auth — IP fallback means second is throttled.
        await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.5"));
        var blocked = NewContext(remoteIp: "10.0.0.5");
        await mw.InvokeAsync(blocked);

        next.CallCount.Should().Be(1);
        blocked.Response.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
    }

    [Fact]
    public async Task PerIpAndUserMode_PartitionsByCompositeKey()
    {
        var options = LoginRule(1);
        options.Rules[0].Mode = RateLimitMode.PerIpAndUser;
        var (mw, next) = Build(options);

        var u1 = MakeAuthenticatedUser("u1");
        // Same user, different IPs → 2 buckets.
        await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.1", user: u1));
        await mw.InvokeAsync(NewContext(remoteIp: "10.0.0.2", user: u1));

        next.CallCount.Should().Be(2);
    }

    private static ClaimsPrincipal MakeAuthenticatedUser(string userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId) }, "Test");
        return new ClaimsPrincipal(identity);
    }
}
