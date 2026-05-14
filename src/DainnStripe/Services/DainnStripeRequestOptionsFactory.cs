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
        var ctx = _requestContextAccessor.Current;
        var hasAccount = !string.IsNullOrWhiteSpace(ctx.StripeAccountId);
        var hasIdempotencyKey = !string.IsNullOrWhiteSpace(ctx.IdempotencyKey);

        if (!hasAccount && !hasIdempotencyKey)
        {
            return null;
        }

        var options = new RequestOptions();
        if (hasAccount) options.StripeAccount = ctx.StripeAccountId;
        if (hasIdempotencyKey) options.IdempotencyKey = ctx.IdempotencyKey;
        return options;
    }
}
