using DainnStripe.Configuration;
using FluentAssertions;

namespace DainnStripe.UnitTests.Configuration;

public class DainnStripeOptionsTests
{
    [Fact]
    public void ToString_DoesNotExposeSecretKey()
    {
        var options = new DainnStripeOptions
        {
            SecretKey = "sk_live_supersecret_key_1234567890",
            WebhookSigningSecret = "whsec_anothersecret"
        };

        var result = options.ToString();

        result.Should().NotContain("sk_live_supersecret_key_1234567890");
        result.Should().NotContain("whsec_anothersecret");
    }

    [Fact]
    public void ToString_DoesNotExposeWebhookSigningSecret()
    {
        var options = new DainnStripeOptions
        {
            SecretKey = "sk_test_abc",
            WebhookSigningSecret = "whsec_do_not_log_this"
        };

        options.ToString().Should().NotContain("whsec_do_not_log_this");
    }

    [Fact]
    public void ToString_IncludesSafeKeyHint()
    {
        var options = new DainnStripeOptions
        {
            SecretKey = "sk_test_12345678abcdef",
            WebhookSigningSecret = "whsec_secret"
        };

        var result = options.ToString();

        result.Should().Contain("sk_test_");
        result.Should().Contain("[redacted]");
    }

    [Fact]
    public void ToString_WhenSecretKeyNotSet_ShowsNotSet()
    {
        var options = new DainnStripeOptions { SecretKey = "" };

        options.ToString().Should().Contain("[not set]");
    }
}
