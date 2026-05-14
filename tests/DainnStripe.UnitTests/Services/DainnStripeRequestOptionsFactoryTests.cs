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
}
