using DainnStripe.Data;
using DainnStripe.Enums;
using DainnStripe.Exceptions;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace DainnStripe.UnitTests.Services;

public class StripeWebhookIdempotencyServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DainnStripeDbContext _dbContext;
    private readonly StripeWebhookIdempotencyService _service;

    public StripeWebhookIdempotencyServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<DainnStripeDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new DainnStripeDbContext(options);
        _dbContext.Database.EnsureCreated();
        _service = new StripeWebhookIdempotencyService(_dbContext);
    }

    [Fact]
    public async Task GetOrCreateAsync_NewEvent_CreatesRecordWithPayloadHash()
    {
        var stripeEvent = BuildEvent("evt_new", "customer.created");
        var payload = BuildPayload("evt_new", "customer.created");

        var record = await _service.GetOrCreateAsync(stripeEvent, payload);

        record.StripeEventId.Should().Be("evt_new");
        record.PayloadHash.Should().HaveLength(64);
        record.PayloadHash.Should().MatchRegex("^[0-9a-f]+$");
        record.Status.Should().Be(StripeWebhookProcessingStatus.Received);
    }

    [Fact]
    public async Task GetOrCreateAsync_SameEventSamePayload_ReturnsSameRecord()
    {
        var stripeEvent = BuildEvent("evt_dup", "customer.created");
        var payload = BuildPayload("evt_dup", "customer.created");

        var first = await _service.GetOrCreateAsync(stripeEvent, payload);
        var second = await _service.GetOrCreateAsync(stripeEvent, payload);

        second.Id.Should().Be(first.Id);
        (await _dbContext.StripeWebhookEvents.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreateAsync_SameEventDifferentPayload_ThrowsFingerprintConflict()
    {
        var stripeEvent = BuildEvent("evt_conflict", "customer.created");
        var originalPayload = BuildPayload("evt_conflict", "customer.created");
        var alteredPayload = BuildPayload("evt_conflict", "customer.updated");

        await _service.GetOrCreateAsync(stripeEvent, originalPayload);

        var act = async () => await _service.GetOrCreateAsync(stripeEvent, alteredPayload);

        await act.Should().ThrowAsync<StripeWebhookFingerprintConflictException>()
            .WithMessage("*evt_conflict*");
    }

    [Fact]
    public async Task GetOrCreateAsync_FingerprintConflict_ExposesEventId()
    {
        var stripeEvent = BuildEvent("evt_fp", "payment_intent.created");
        var payload1 = BuildPayload("evt_fp", "payment_intent.created");
        var payload2 = BuildPayload("evt_fp", "payment_intent.succeeded");

        await _service.GetOrCreateAsync(stripeEvent, payload1);

        var ex = await Assert.ThrowsAsync<StripeWebhookFingerprintConflictException>(
            () => _service.GetOrCreateAsync(stripeEvent, payload2));

        ex.StripeEventId.Should().Be("evt_fp");
        ex.StoredHashPrefix.Should().HaveLength(8);
        ex.IncomingHashPrefix.Should().HaveLength(8);
        ex.StoredHashPrefix.Should().NotBe(ex.IncomingHashPrefix);
    }

    [Fact]
    public async Task MarkProcessingAsync_IncrementsAttemptsAndSetsStatus()
    {
        var stripeEvent = BuildEvent("evt_proc", "invoice.payment_succeeded");
        var payload = BuildPayload("evt_proc", "invoice.payment_succeeded");
        var record = await _service.GetOrCreateAsync(stripeEvent, payload);

        await _service.MarkProcessingAsync(record);

        record.Status.Should().Be(StripeWebhookProcessingStatus.Processing);
        record.Attempts.Should().Be(1);
    }

    [Fact]
    public async Task MarkProcessedAsync_SetsStatusAndProcessedAt()
    {
        var stripeEvent = BuildEvent("evt_done", "invoice.paid");
        var payload = BuildPayload("evt_done", "invoice.paid");
        var record = await _service.GetOrCreateAsync(stripeEvent, payload);
        await _service.MarkProcessingAsync(record);

        await _service.MarkProcessedAsync(record);

        record.Status.Should().Be(StripeWebhookProcessingStatus.Processed);
        record.ProcessedAt.Should().NotBeNull();
        record.ErrorMessage.Should().BeNull();
        _service.IsProcessed(record).Should().BeTrue();
    }

    [Fact]
    public async Task MarkFailedAsync_SetsStatusAndErrorMessage()
    {
        var stripeEvent = BuildEvent("evt_fail", "charge.failed");
        var payload = BuildPayload("evt_fail", "charge.failed");
        var record = await _service.GetOrCreateAsync(stripeEvent, payload);

        await _service.MarkFailedAsync(record, "downstream timeout");

        record.Status.Should().Be(StripeWebhookProcessingStatus.Failed);
        record.ErrorMessage.Should().Be("downstream timeout");
        _service.IsProcessed(record).Should().BeFalse();
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static Event BuildEvent(string id, string type) => new()
    {
        Id = id,
        Type = type,
        ApiVersion = "2024-06-20",
        Livemode = false
    };

    private static string BuildPayload(string id, string type) => $$"""
        {
          "id": "{{id}}",
          "object": "event",
          "api_version": "2024-06-20",
          "created": 1710000000,
          "type": "{{type}}",
          "livemode": false,
          "data": { "object": { "id": "obj_test", "object": "customer" } }
        }
        """;
}
