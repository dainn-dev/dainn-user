using System.Security.Cryptography;
using System.Text;
using DainnStripe.Configuration;
using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Stripe;

namespace DainnStripe.UnitTests.Services;

public class StripeWebhookServiceTests
{
    private const string SigningSecret = "whsec_test_secret";

    [Fact]
    public async Task ProcessAsync_NewEvent_PersistsAndDispatchesHandler()
    {
        await using var fixture = await CreateFixtureAsync(new RecordingHandler("customer.created"));
        var payload = CreatePayload("evt_1", "customer.created");

        var result = await fixture.Service.ProcessAsync(payload, CreateSignatureHeader(payload));

        result.IsDuplicate.Should().BeFalse();
        result.HandlerCount.Should().Be(1);
        result.EventRecord.StripeEventId.Should().Be("evt_1");
        result.EventRecord.Status.Should().Be(StripeWebhookProcessingStatus.Processed);
        fixture.Handler.HandledEventIds.Should().ContainSingle("evt_1");

        var saved = await fixture.DbContext.StripeWebhookEvents.SingleAsync();
        saved.ProcessedAt.Should().NotBeNull();
        saved.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_ExistingEvent_ReturnsDuplicateWithoutDispatching()
    {
        await using var fixture = await CreateFixtureAsync(new RecordingHandler("customer.created"));
        var payload = CreatePayload("evt_duplicate", "customer.created");

        await fixture.Service.ProcessAsync(payload, CreateSignatureHeader(payload));
        var second = await fixture.Service.ProcessAsync(payload, CreateSignatureHeader(payload));

        second.IsDuplicate.Should().BeTrue();
        second.HandlerCount.Should().Be(0);
        fixture.Handler.HandledEventIds.Should().ContainSingle("evt_duplicate");
        (await fixture.DbContext.StripeWebhookEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_HandlerThrows_MarksEventFailed()
    {
        await using var fixture = await CreateFixtureAsync(new ThrowingHandler("invoice.payment_failed"));
        var payload = CreatePayload("evt_failed", "invoice.payment_failed");

        await fixture.Service.Invoking(service => service.ProcessAsync(payload, CreateSignatureHeader(payload)))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("handler failed");

        var saved = await fixture.DbContext.StripeWebhookEvents.SingleAsync();
        saved.Status.Should().Be(StripeWebhookProcessingStatus.Failed);
        saved.ErrorMessage.Should().Be("handler failed");
        saved.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task ProcessAsync_FailedEventRetry_ReprocessesExistingRecord()
    {
        await using var fixture = await CreateFixtureAsync(new ThrowOnceHandler("invoice.payment_succeeded"));
        var payload = CreatePayload("evt_retry", "invoice.payment_succeeded");

        await fixture.Service.Invoking(service => service.ProcessAsync(payload, CreateSignatureHeader(payload)))
            .Should().ThrowAsync<InvalidOperationException>();

        var retry = await fixture.Service.ProcessAsync(payload, CreateSignatureHeader(payload));

        retry.IsDuplicate.Should().BeFalse();
        retry.HandlerCount.Should().Be(1);

        var saved = await fixture.DbContext.StripeWebhookEvents.SingleAsync();
        saved.Status.Should().Be(StripeWebhookProcessingStatus.Processed);
        saved.Attempts.Should().Be(2);
        saved.ErrorMessage.Should().BeNull();
    }

    private static async Task<TestFixture> CreateFixtureAsync(IStripeWebhookHandler handler)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<DainnStripeDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new DainnStripeDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var stripeOptions = new DainnStripeOptions
        {
            SecretKey = "sk_test_123",
            WebhookSigningSecret = SigningSecret
        };

        var idempotency = new StripeWebhookIdempotencyService(dbContext);
        var service = new StripeWebhookService(
            idempotency,
            new[] { handler },
            stripeOptions,
            NullLogger<StripeWebhookService>.Instance);
        var recordingHandler = handler as RecordingHandler ?? new RecordingHandler("unused");

        return new TestFixture(connection, dbContext, service, recordingHandler);
    }

    private static string CreatePayload(string eventId, string eventType)
    {
        return $$"""
        {
          "id": "{{eventId}}",
          "object": "event",
          "api_version": "2024-06-20",
          "created": 1710000000,
          "data": {
            "object": {
              "id": "cus_test",
              "object": "customer"
            }
          },
          "livemode": false,
          "pending_webhooks": 1,
          "request": {
            "id": "req_test",
            "idempotency_key": null
          },
          "type": "{{eventType}}"
        }
        """;
    }

    private static string CreateSignatureHeader(string payload)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(SigningSecret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload))).ToLowerInvariant();
        return $"t={timestamp},v1={signature}";
    }

    private sealed class RecordingHandler : IStripeWebhookHandler
    {
        private readonly string _eventType;

        public RecordingHandler(string eventType)
        {
            _eventType = eventType;
        }

        public List<string> HandledEventIds { get; } = new();

        public bool CanHandle(string eventType) => eventType == _eventType;

        public Task HandleAsync(Event stripeEvent, StripeWebhookEventRecord record, CancellationToken cancellationToken = default)
        {
            HandledEventIds.Add(stripeEvent.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingHandler : IStripeWebhookHandler
    {
        private readonly string _eventType;

        public ThrowingHandler(string eventType)
        {
            _eventType = eventType;
        }

        public bool CanHandle(string eventType) => eventType == _eventType;

        public Task HandleAsync(Event stripeEvent, StripeWebhookEventRecord record, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("handler failed");
        }
    }

    private sealed class ThrowOnceHandler : IStripeWebhookHandler
    {
        private readonly string _eventType;
        private bool _hasThrown;

        public ThrowOnceHandler(string eventType)
        {
            _eventType = eventType;
        }

        public bool CanHandle(string eventType) => eventType == _eventType;

        public Task HandleAsync(Event stripeEvent, StripeWebhookEventRecord record, CancellationToken cancellationToken = default)
        {
            if (!_hasThrown)
            {
                _hasThrown = true;
                throw new InvalidOperationException("handler failed once");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        public TestFixture(
            SqliteConnection connection,
            DainnStripeDbContext dbContext,
            StripeWebhookService service,
            RecordingHandler handler)
        {
            _connection = connection;
            DbContext = dbContext;
            Service = service;
            Handler = handler;
        }

        public DainnStripeDbContext DbContext { get; }

        public StripeWebhookService Service { get; }

        public RecordingHandler Handler { get; }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
