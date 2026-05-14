using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Stripe.net backed PaymentIntent client.
/// </summary>
public class DainnStripePaymentIntentClient : IDainnStripePaymentIntentClient
{
    private readonly PaymentIntentService _paymentIntentService;
    private readonly IDainnStripeRequestOptionsFactory _requestOptionsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePaymentIntentClient"/> class.
    /// </summary>
    public DainnStripePaymentIntentClient()
        : this(new PaymentIntentService(), new DainnStripeRequestOptionsFactory(new DainnStripeRequestContextAccessor()))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePaymentIntentClient"/> class.
    /// </summary>
    public DainnStripePaymentIntentClient(IDainnStripeRequestOptionsFactory requestOptionsFactory)
        : this(new PaymentIntentService(), requestOptionsFactory)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripePaymentIntentClient"/> class.
    /// </summary>
    public DainnStripePaymentIntentClient(
        PaymentIntentService paymentIntentService,
        IDainnStripeRequestOptionsFactory requestOptionsFactory)
    {
        _paymentIntentService = paymentIntentService;
        _requestOptionsFactory = requestOptionsFactory;
    }

    /// <inheritdoc />
    public async Task<PaymentIntentResult> CreateAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new PaymentIntentCreateOptions
        {
            Amount = request.Amount,
            Currency = request.Currency,
            Customer = request.StripeCustomerId,
            Description = request.Description,
            ApplicationFeeAmount = request.ApplicationFeeAmount,
            TransferGroup = request.TransferGroup,
            TransferData = string.IsNullOrWhiteSpace(request.TransferDestinationAccountId)
                ? null
                : new PaymentIntentTransferDataOptions
                {
                    Destination = request.TransferDestinationAccountId,
                    Amount = request.TransferAmount
                },
            Metadata = request.Metadata.Count == 0
                ? null
                : new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            }
        };

        var paymentIntent = await _paymentIntentService.CreateAsync(options, _requestOptionsFactory.Create(), cancellationToken);

        return new PaymentIntentResult
        {
            PaymentIntentId = paymentIntent.Id,
            ClientSecret = paymentIntent.ClientSecret,
            StripeCustomerId = paymentIntent.CustomerId,
            Amount = paymentIntent.Amount,
            Currency = paymentIntent.Currency,
            Status = paymentIntent.Status
        };
    }
}
