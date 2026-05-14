using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;
using Stripe.Checkout;

namespace DainnStripe.Services;

/// <summary>
/// Stripe.net backed Checkout session client.
/// </summary>
public class DainnStripeCheckoutSessionClient : IDainnStripeCheckoutSessionClient
{
    private readonly SessionService _sessionService;
    private readonly IDainnStripeRequestOptionsFactory _requestOptionsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeCheckoutSessionClient"/> class.
    /// </summary>
    public DainnStripeCheckoutSessionClient()
        : this(new SessionService(), new DainnStripeRequestOptionsFactory(new DainnStripeRequestContextAccessor()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeCheckoutSessionClient"/> class.
    /// </summary>
    public DainnStripeCheckoutSessionClient(IDainnStripeRequestOptionsFactory requestOptionsFactory)
        : this(new SessionService(), requestOptionsFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeCheckoutSessionClient"/> class.
    /// </summary>
    public DainnStripeCheckoutSessionClient(
        SessionService sessionService,
        IDainnStripeRequestOptionsFactory requestOptionsFactory)
    {
        _sessionService = sessionService;
        _requestOptionsFactory = requestOptionsFactory;
    }

    /// <inheritdoc />
    public async Task<CheckoutSessionResult> CreateAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new SessionCreateOptions
        {
            Mode = ToStripeMode(request.Mode),
            Customer = request.StripeCustomerId,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            ClientReferenceId = request.OwnerId,
            Metadata = request.Metadata.Count == 0
                ? null
                : new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal),
            LineItems = request.LineItems
                .Select(item => new SessionLineItemOptions
                {
                    Price = item.StripePriceId,
                    Quantity = item.Quantity
                })
                .ToList()
        };

        var session = await _sessionService.CreateAsync(options, _requestOptionsFactory.Create(), cancellationToken);

        return new CheckoutSessionResult
        {
            SessionId = session.Id,
            Url = session.Url,
            StripeCustomerId = session.CustomerId,
            StripePaymentIntentId = session.PaymentIntentId,
            StripeSubscriptionId = session.SubscriptionId
        };
    }

    private static string ToStripeMode(DainnStripeCheckoutMode mode)
    {
        return mode switch
        {
            DainnStripeCheckoutMode.Payment => "payment",
            DainnStripeCheckoutMode.Subscription => "subscription",
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unsupported checkout mode.")
        };
    }
}
