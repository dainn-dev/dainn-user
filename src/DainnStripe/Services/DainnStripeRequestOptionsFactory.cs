using DainnStripe.Interfaces;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Default Stripe request options factory.
/// </summary>
public class DainnStripeRequestOptionsFactory : IDainnStripeRequestOptionsFactory
{
    private readonly IDainnStripeRequestContextAccessor _requestContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeRequestOptionsFactory"/> class.
    /// </summary>
    public DainnStripeRequestOptionsFactory(IDainnStripeRequestContextAccessor requestContextAccessor)
    {
        _requestContextAccessor = requestContextAccessor;
    }

    /// <inheritdoc />
    public RequestOptions? Create()
    {
        var stripeAccountId = _requestContextAccessor.Current.StripeAccountId;
        if (string.IsNullOrWhiteSpace(stripeAccountId))
        {
            return null;
        }

        return new RequestOptions
        {
            StripeAccount = stripeAccountId
        };
    }
}
