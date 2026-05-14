using DainnStripe;
using DainnStripe.Configuration;
using DainnStripe.Interfaces;
using DainnStripe.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DainnStripe.UnitTests.Infrastructure;

public class DainnStripeServiceExtensionsTests
{
    [Fact]
    public void AddDainnStripe_WithValidConfiguration_RegistersFoundationServices()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration();

        services.AddLogging();
        services.AddDainnStripe(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<DainnStripeOptions>().SecretKey.Should().Be("sk_test_123");
        provider.GetRequiredService<IDainnStripeClientFactory>().Should().BeOfType<DainnStripeClientFactory>();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IDainnStripeCatalogService>().Should().BeOfType<DainnStripeCatalogService>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeConnectAccountClient>().Should().BeOfType<DainnStripeConnectAccountClient>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeConnectService>().Should().BeOfType<DainnStripeConnectService>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeRequestContextAccessor>().Should().BeOfType<DainnStripeRequestContextAccessor>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeRequestOptionsFactory>().Should().BeOfType<DainnStripeRequestOptionsFactory>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeCheckoutSessionClient>().Should().BeOfType<DainnStripeCheckoutSessionClient>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeCheckoutService>().Should().BeOfType<DainnStripeCheckoutService>();
        scope.ServiceProvider.GetRequiredService<IDainnStripePaymentIntentClient>().Should().BeOfType<DainnStripePaymentIntentClient>();
        scope.ServiceProvider.GetRequiredService<IDainnStripePaymentService>().Should().BeOfType<DainnStripePaymentService>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeTransferClient>().Should().BeOfType<DainnStripeTransferClient>();
        scope.ServiceProvider.GetRequiredService<IDainnStripePayoutClient>().Should().BeOfType<DainnStripePayoutClient>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeBalanceClient>().Should().BeOfType<DainnStripeBalanceClient>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeMoneyMovementService>().Should().BeOfType<DainnStripeMoneyMovementService>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeReconciliationService>().Should().BeOfType<DainnStripeReconciliationService>();
        scope.ServiceProvider.GetRequiredService<IDainnStripeSubscriptionService>().Should().BeOfType<DainnStripeSubscriptionService>();
        scope.ServiceProvider.GetServices<IStripeWebhookHandler>().Should()
            .ContainSingle(handler => handler.GetType() == typeof(DainnStripePaymentWebhookHandler));
        scope.ServiceProvider.GetServices<IStripeWebhookHandler>().Should()
            .ContainSingle(handler => handler.GetType() == typeof(DainnStripeConnectAccountWebhookHandler));
        scope.ServiceProvider.GetServices<IStripeWebhookHandler>().Should()
            .ContainSingle(handler => handler.GetType() == typeof(DainnStripePayoutWebhookHandler));
        scope.ServiceProvider.GetServices<IStripeWebhookHandler>().Should()
            .ContainSingle(handler => handler.GetType() == typeof(DainnStripeBalanceWebhookHandler));
        scope.ServiceProvider.GetRequiredService<IStripeWebhookIdempotencyService>().Should().BeOfType<StripeWebhookIdempotencyService>();
        scope.ServiceProvider.GetRequiredService<IStripeWebhookService>().Should().BeOfType<StripeWebhookService>();
    }

    [Fact]
    public void MapDainnStripeConnectEndpoints_RegistersConnectRoutes()
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        builder.Services.AddAuthorization();
        builder.Services.AddDainnStripe(CreateConfiguration());

        using var app = builder.Build();
        app.MapDainnStripeConnectEndpoints(requireAuthorization: false);

        var endpoints = ((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .Select(endpoint => endpoint.DisplayName)
            .ToList();

        endpoints.Should().Contain(displayName => displayName!.Contains("POST /dainnstripe/connect/tenants"));
        endpoints.Should().Contain(displayName => displayName!.Contains("POST /dainnstripe/connect/accounts"));
        endpoints.Should().Contain(displayName => displayName!.Contains("POST /dainnstripe/connect/account-links"));
    }

    [Fact]
    public void MapDainnStripeMoneyMovementEndpoints_RegistersMoneyMovementRoutes()
    {
        var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder();
        builder.Services.AddLogging();
        builder.Services.AddAuthorization();
        builder.Services.AddDainnStripe(CreateConfiguration());

        using var app = builder.Build();
        app.MapDainnStripeMoneyMovementEndpoints(requireAuthorization: false);

        var endpoints = ((Microsoft.AspNetCore.Routing.IEndpointRouteBuilder)app)
            .DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .Select(endpoint => endpoint.DisplayName)
            .ToList();

        endpoints.Should().Contain(displayName => displayName!.Contains("POST /dainnstripe/money-movement/transfers"));
        endpoints.Should().Contain(displayName => displayName!.Contains("POST /dainnstripe/money-movement/payouts"));
        endpoints.Should().Contain(displayName => displayName!.Contains("POST /dainnstripe/money-movement/reconcile"));
    }

    [Fact]
    public void AddDainnStripe_WithMissingSecretKey_ThrowsSafeMessage()
    {
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(("DainnStripe:SecretKey", ""));

        services.Invoking(s => s.AddDainnStripe(configuration))
            .Should().Throw<InvalidOperationException>()
            .WithMessage("DainnStripe secret key is not configured*");
    }

    [Fact]
    public void CreateClient_UsesConfiguredSecretKey()
    {
        var factory = new DainnStripeClientFactory(new DainnStripeOptions
        {
            SecretKey = "sk_test_123"
        });

        var client = factory.CreateClient();

        client.ApiKey.Should().Be("sk_test_123");
    }

    private static IConfiguration CreateConfiguration(params (string Key, string Value)[] overrides)
    {
        var values = new Dictionary<string, string?>
        {
            ["DainnStripe:SecretKey"] = "sk_test_123",
            ["DainnStripe:WebhookSigningSecret"] = "whsec_test",
            ["DainnStripe:Database:Provider"] = "SQLite",
            ["DainnStripe:Database:ConnectionString"] = "Data Source=:memory:"
        };

        foreach (var (key, value) in overrides)
        {
            values[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
