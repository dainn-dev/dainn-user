using DainnStripe.Services;
using FluentAssertions;

namespace DainnStripe.UnitTests.Services;

public class DainnStripeRequestOptionsFactoryTests
{
    [Fact]
    public void Create_WithConnectedAccountContext_ReturnsStripeAccountRequestOptions()
    {
        var accessor = new DainnStripeRequestContextAccessor();
        accessor.SetConnectedAccount("acct_1", "tenant_1");
        var factory = new DainnStripeRequestOptionsFactory(accessor);

        var options = factory.Create();

        options.Should().NotBeNull();
        options!.StripeAccount.Should().Be("acct_1");
    }

    [Fact]
    public void Create_WithoutConnectedAccountContext_ReturnsNull()
    {
        var factory = new DainnStripeRequestOptionsFactory(new DainnStripeRequestContextAccessor());

        factory.Create().Should().BeNull();
    }

    [Fact]
    public void Create_WithIdempotencyKey_IncludesKeyInOptions()
    {
        var accessor = new DainnStripeRequestContextAccessor();
        accessor.SetIdempotencyKey("idem_key_abc123");
        var factory = new DainnStripeRequestOptionsFactory(accessor);

        var options = factory.Create();

        options.Should().NotBeNull();
        options!.IdempotencyKey.Should().Be("idem_key_abc123");
        options.StripeAccount.Should().BeNull();
    }

    [Fact]
    public void Create_WithConnectedAccountAndIdempotencyKey_IncludesBoth()
    {
        var accessor = new DainnStripeRequestContextAccessor();
        accessor.SetConnectedAccount("acct_1", "tenant_1");
        accessor.SetIdempotencyKey("idem_key_xyz");
        var factory = new DainnStripeRequestOptionsFactory(accessor);

        var options = factory.Create();

        options.Should().NotBeNull();
        options!.StripeAccount.Should().Be("acct_1");
        options.IdempotencyKey.Should().Be("idem_key_xyz");
    }

    [Fact]
    public void Clear_ResetsIdempotencyKey()
    {
        var accessor = new DainnStripeRequestContextAccessor();
        accessor.SetIdempotencyKey("idem_key_abc");
        accessor.Clear();

        var factory = new DainnStripeRequestOptionsFactory(accessor);

        factory.Create().Should().BeNull();
    }
}
