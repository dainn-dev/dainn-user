using DainnStripe.Configuration;
using DainnStripe.Interfaces;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Default Stripe client factory.
/// </summary>
public class DainnStripeClientFactory : IDainnStripeClientFactory
{
    private readonly DainnStripeOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeClientFactory"/> class.
    /// </summary>
    public DainnStripeClientFactory(DainnStripeOptions options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public IStripeClient CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
        {
            throw new InvalidOperationException("DainnStripe secret key is not configured.");
        }

        return new StripeClient(_options.SecretKey);
    }
}
