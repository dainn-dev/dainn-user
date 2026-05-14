using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;

namespace DainnStripe.Services;

/// <summary>
/// Default PaymentIntent orchestration service.
/// </summary>
public class DainnStripePaymentService : IDainnStripePaymentService
{
    private readonly IDainnStripePaymentIntentClient _paymentIntentClient;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePaymentService"/> class.
    /// </summary>
    public DainnStripePaymentService(
        IDainnStripePaymentIntentClient paymentIntentClient,
        DainnStripeDbContext dbContext)
    {
        _paymentIntentClient = paymentIntentClient;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.OwnerId, nameof(request.OwnerId));
        EnsureRequired(request.Currency, nameof(request.Currency));

        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount must be greater than zero.");
        }

        if (request.ApplicationFeeAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request.ApplicationFeeAmount),
                "Application fee amount cannot be negative.");
        }

        if (request.TransferAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.TransferAmount), "Transfer amount cannot be negative.");
        }

        var result = await _paymentIntentClient.CreateAsync(request, cancellationToken);
        var now = DateTime.UtcNow;

        _dbContext.DainnStripePayments.Add(new DainnStripePayment
        {
            Id = Guid.NewGuid(),
            OwnerId = request.OwnerId,
            StripePaymentIntentId = result.PaymentIntentId,
            StripeCustomerId = result.StripeCustomerId ?? request.StripeCustomerId,
            Amount = result.Amount == 0 ? request.Amount : result.Amount,
            Currency = string.IsNullOrWhiteSpace(result.Currency)
                ? request.Currency.ToLowerInvariant()
                : result.Currency.ToLowerInvariant(),
            Status = DainnStripePaymentStatus.Pending,
            MetadataJson = request.Metadata.TryGetValue("metadata_json", out var metadataJson) ? metadataJson : null,
            CreatedAt = now,
            UpdatedAt = now
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
