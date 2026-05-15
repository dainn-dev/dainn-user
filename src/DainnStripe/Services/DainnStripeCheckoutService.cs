using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;

namespace DainnStripe.Services;

/// <summary>
/// Default Checkout orchestration service.
/// </summary>
public class DainnStripeCheckoutService : IDainnStripeCheckoutService
{
    private readonly IDainnStripeCheckoutSessionClient _checkoutSessionClient;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeCheckoutService"/> class.
    /// </summary>
    public DainnStripeCheckoutService(
        IDainnStripeCheckoutSessionClient checkoutSessionClient,
        DainnStripeDbContext dbContext)
    {
        _checkoutSessionClient = checkoutSessionClient;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<CheckoutSessionResult> CreateAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureValid(request);

        var result = await _checkoutSessionClient.CreateAsync(request, cancellationToken);
        var now = DateTime.UtcNow;

        if (request.Mode == DainnStripeCheckoutMode.Payment)
        {
            _dbContext.DainnStripePayments.Add(new DainnStripePayment
            {
                Id = Guid.NewGuid(),
                OwnerId = request.OwnerId,
                StripeCheckoutSessionId = result.SessionId,
                StripePaymentIntentId = result.StripePaymentIntentId,
                StripeCustomerId = result.StripeCustomerId ?? request.StripeCustomerId,
                Amount = request.LineItems.Sum(item => (item.UnitAmount ?? 0) * item.Quantity),
                Currency = ResolveCurrency(request),
                Status = DainnStripePaymentStatus.Pending,
                MetadataJson = TryGetMetadataJson(request),
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else if (request.Mode == DainnStripeCheckoutMode.Subscription)
        {
            if (string.IsNullOrWhiteSpace(result.StripeSubscriptionId))
            {
                throw new InvalidOperationException(
                    $"Stripe checkout session '{result.SessionId}' was created in subscription mode but returned no subscription ID.");
            }

            var customerId = result.StripeCustomerId ?? request.StripeCustomerId;
            if (string.IsNullOrWhiteSpace(customerId))
            {
                throw new InvalidOperationException(
                    $"Stripe checkout session '{result.SessionId}' returned no customer ID for subscription.");
            }

            _dbContext.DainnStripeSubscriptions.Add(new DainnStripeSubscription
            {
                Id = Guid.NewGuid(),
                OwnerId = request.OwnerId,
                StripeSubscriptionId = result.StripeSubscriptionId,
                StripeCustomerId = customerId,
                StripePriceId = request.LineItems.FirstOrDefault()?.StripePriceId,
                Status = DainnStripeSubscriptionStatus.Incomplete,
                MetadataJson = TryGetMetadataJson(request),
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static void EnsureValid(CreateCheckoutSessionRequest request)
    {
        EnsureRequired(request.OwnerId, nameof(request.OwnerId));
        EnsureRequired(request.SuccessUrl, nameof(request.SuccessUrl));
        EnsureRequired(request.CancelUrl, nameof(request.CancelUrl));

        if (request.LineItems.Count == 0)
        {
            throw new ArgumentException("At least one checkout line item is required.", nameof(request.LineItems));
        }

        foreach (var lineItem in request.LineItems)
        {
            EnsureRequired(lineItem.StripePriceId, nameof(lineItem.StripePriceId));
            if (lineItem.Quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(lineItem.Quantity), "Quantity must be greater than zero.");
            }
        }
    }

    private static string ResolveCurrency(CreateCheckoutSessionRequest request)
    {
        return request.LineItems.FirstOrDefault(item => !string.IsNullOrWhiteSpace(item.Currency))?.Currency
            ?.ToLowerInvariant() ?? "usd";
    }

    private static string? TryGetMetadataJson(CreateCheckoutSessionRequest request)
    {
        return request.Metadata.TryGetValue("metadata_json", out var metadataJson)
            ? metadataJson
            : null;
    }

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
