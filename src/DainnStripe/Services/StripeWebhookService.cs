using DainnStripe.Configuration;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.Extensions.Logging;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Default Stripe webhook service.
/// </summary>
public class StripeWebhookService : IStripeWebhookService
{
    private readonly IStripeWebhookIdempotencyService _idempotency;
    private readonly IEnumerable<IStripeWebhookHandler> _handlers;
    private readonly DainnStripeOptions _options;
    private readonly ILogger<StripeWebhookService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StripeWebhookService"/> class.
    /// </summary>
    public StripeWebhookService(
        IStripeWebhookIdempotencyService idempotency,
        IEnumerable<IStripeWebhookHandler> handlers,
        DainnStripeOptions options,
        ILogger<StripeWebhookService> logger)
    {
        _idempotency = idempotency;
        _handlers = handlers;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StripeWebhookProcessResult> ProcessAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("DainnStripe is disabled.");
        }

        if (string.IsNullOrWhiteSpace(_options.WebhookSigningSecret))
        {
            throw new InvalidOperationException("Stripe webhook signing secret is not configured.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentException.ThrowIfNullOrWhiteSpace(signatureHeader);

        var stripeEvent = EventUtility.ConstructEvent(
            payload,
            signatureHeader,
            _options.WebhookSigningSecret,
            _options.WebhookToleranceSeconds,
            _options.ThrowOnApiVersionMismatch);

        var record = await _idempotency.GetOrCreateAsync(stripeEvent, payload, cancellationToken);
        if (_idempotency.IsProcessed(record))
        {
            _logger.LogInformation(
                "Duplicate Stripe webhook event ignored. EventId={StripeEventId} EventType={EventType}",
                record.StripeEventId,
                record.EventType);

            return new StripeWebhookProcessResult
            {
                EventRecord = record,
                IsDuplicate = true,
                HandlerCount = 0
            };
        }

        var handlerCount = 0;
        try
        {
            await _idempotency.MarkProcessingAsync(record, cancellationToken);

            foreach (var handler in _handlers.Where(handler => handler.CanHandle(stripeEvent.Type)))
            {
                _logger.LogInformation(
                    "Dispatching Stripe webhook event. EventId={StripeEventId} EventType={EventType} Handler={Handler}",
                    record.StripeEventId,
                    record.EventType,
                    handler.GetType().Name);

                await handler.HandleAsync(stripeEvent, record, cancellationToken);
                handlerCount++;
            }

            await _idempotency.MarkProcessedAsync(record, cancellationToken);
            _logger.LogInformation(
                "Stripe webhook event processed. EventId={StripeEventId} EventType={EventType} HandlerCount={HandlerCount}",
                record.StripeEventId,
                record.EventType,
                handlerCount);

            return new StripeWebhookProcessResult
            {
                EventRecord = record,
                IsDuplicate = false,
                HandlerCount = handlerCount
            };
        }
        catch (Exception ex)
        {
            await _idempotency.MarkFailedAsync(record, ex.Message, CancellationToken.None);
            _logger.LogError(
                ex,
                "Stripe webhook event processing failed. EventId={StripeEventId} EventType={EventType}",
                record.StripeEventId,
                record.EventType);
            throw;
        }
    }
}
