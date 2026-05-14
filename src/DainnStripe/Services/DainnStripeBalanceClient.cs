using System.Text.Json;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Stripe.net backed Balance client.
/// </summary>
public class DainnStripeBalanceClient : IDainnStripeBalanceClient
{
    private readonly BalanceService _balanceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeBalanceClient"/> class.
    /// </summary>
    public DainnStripeBalanceClient()
        : this(new BalanceService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeBalanceClient"/> class.
    /// </summary>
    public DainnStripeBalanceClient(BalanceService balanceService)
    {
        _balanceService = balanceService;
    }

    /// <inheritdoc />
    public async Task<BalanceSnapshotResult> GetAsync(
        string? stripeAccountId = null,
        CancellationToken cancellationToken = default)
    {
        var requestOptions = string.IsNullOrWhiteSpace(stripeAccountId)
            ? null
            : new RequestOptions { StripeAccount = stripeAccountId };

        var balance = await _balanceService.GetAsync(requestOptions, cancellationToken);

        return new BalanceSnapshotResult
        {
            StripeAccountId = stripeAccountId,
            AvailableJson = JsonSerializer.Serialize(balance.Available),
            PendingJson = JsonSerializer.Serialize(balance.Pending),
            Livemode = balance.Livemode
        };
    }
}
