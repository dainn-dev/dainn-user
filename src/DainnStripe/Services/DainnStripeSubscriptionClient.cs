using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Stripe API client for subscription create and cancel operations.
/// </summary>
public class DainnStripeSubscriptionClient : IDainnStripeSubscriptionClient
{
    private readonly IDainnStripeClientFactory _clientFactory;
    private readonly IDainnStripeRequestOptionsFactory _requestOptionsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeSubscriptionClient"/> class.
    /// </summary>
    public DainnStripeSubscriptionClient(
        IDainnStripeClientFactory clientFactory,
        IDainnStripeRequestOptionsFactory requestOptionsFactory)
    {
        _clientFactory = clientFactory;
        _requestOptionsFactory = requestOptionsFactory;
    }

    /// <inheritdoc />
    public async Task<SubscriptionResult> CreateAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.CreateClient();
        var service = new SubscriptionService(client);

        var options = new SubscriptionCreateOptions
        {
            Customer = request.StripeCustomerId,
            Items = new List<SubscriptionItemOptions>
            {
                new() { Price = request.StripePriceId }
            },
            TrialPeriodDays = request.TrialPeriodDays,
            Metadata = request.Metadata.Count > 0 ? request.Metadata : null
        };

        var requestOptions = _requestOptionsFactory.Create();
        var subscription = await service.CreateAsync(options, requestOptions, cancellationToken);
        return ToResult(subscription);
    }

    /// <inheritdoc />
    public async Task<SubscriptionResult> CancelAsync(
        string stripeSubscriptionId,
        CancellationToken cancellationToken = default)
    {
        var client = _clientFactory.CreateClient();
        var service = new SubscriptionService(client);
        var subscription = await service.CancelAsync(stripeSubscriptionId, null, null, cancellationToken);
        return ToResult(subscription);
    }

    private static SubscriptionResult ToResult(Subscription subscription) => new()
    {
        StripeSubscriptionId = subscription.Id,
        StripeCustomerId = subscription.CustomerId,
        Status = MapStatus(subscription.Status),
        CurrentPeriodEnd = subscription.Items?.Data?.FirstOrDefault()?.CurrentPeriodEnd,
        CancelAtPeriodEnd = subscription.CancelAtPeriodEnd
    };

    private static DainnStripeSubscriptionStatus MapStatus(string status) => status switch
    {
        "active" => DainnStripeSubscriptionStatus.Active,
        "trialing" => DainnStripeSubscriptionStatus.Trialing,
        "past_due" => DainnStripeSubscriptionStatus.PastDue,
        "canceled" => DainnStripeSubscriptionStatus.Canceled,
        "unpaid" => DainnStripeSubscriptionStatus.Unpaid,
        "incomplete" => DainnStripeSubscriptionStatus.Incomplete,
        _ => DainnStripeSubscriptionStatus.Unknown
    };
}
