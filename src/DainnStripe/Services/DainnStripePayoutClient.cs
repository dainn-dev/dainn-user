using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Stripe.net backed Payout client.
/// </summary>
public class DainnStripePayoutClient : IDainnStripePayoutClient
{
    private readonly PayoutService _payoutService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePayoutClient"/> class.
    /// </summary>
    public DainnStripePayoutClient()
        : this(new PayoutService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePayoutClient"/> class.
    /// </summary>
    public DainnStripePayoutClient(PayoutService payoutService)
    {
        _payoutService = payoutService;
    }

    /// <inheritdoc />
    public async Task<PayoutResult> CreateAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new PayoutCreateOptions
        {
            Amount = request.Amount,
            Currency = request.Currency,
            Destination = request.Destination,
            Method = request.Method,
            Description = request.Description,
            StatementDescriptor = request.StatementDescriptor,
            Metadata = request.Metadata.Count == 0
                ? null
                : new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal)
        };

        var requestOptions = new RequestOptions
        {
            StripeAccount = request.StripeAccountId,
            IdempotencyKey = request.IdempotencyKey
        };

        var payout = await _payoutService.CreateAsync(options, requestOptions, cancellationToken);

        return new PayoutResult
        {
            PayoutId = payout.Id,
            StripeAccountId = request.StripeAccountId,
            Amount = payout.Amount,
            Currency = payout.Currency,
            Destination = payout.DestinationId ?? request.Destination,
            Method = payout.Method,
            Status = payout.Status,
            ArrivalDate = payout.ArrivalDate
        };
    }

    /// <inheritdoc />
    public async Task<PayoutResult> GetAsync(
        string payoutId,
        string stripeAccountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payoutId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeAccountId);

        var payout = await _payoutService.GetAsync(
            payoutId,
            null,
            new RequestOptions { StripeAccount = stripeAccountId },
            cancellationToken);

        return new PayoutResult
        {
            PayoutId = payout.Id,
            StripeAccountId = stripeAccountId,
            Amount = payout.Amount,
            Currency = payout.Currency,
            Destination = payout.DestinationId,
            Method = payout.Method,
            Status = payout.Status,
            ArrivalDate = payout.ArrivalDate
        };
    }
}
