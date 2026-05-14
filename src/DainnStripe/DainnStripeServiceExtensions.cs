using DainnStripe.Configuration;
using DainnStripe.Data;
using DainnStripe.Interfaces;
using DainnStripe.Middleware;
using DainnStripe.Models;
using DainnStripe.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Stripe;

namespace DainnStripe;

/// <summary>
/// Extension methods for configuring DainnStripe.
/// </summary>
public static class DainnStripeServiceExtensions
{
    /// <summary>
    /// Adds DainnStripe services with configuration from <c>DainnStripe</c>.
    /// </summary>
    public static IServiceCollection AddDainnStripe(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return AddDainnStripe(services, configuration, options => { });
    }

    /// <summary>
    /// Adds DainnStripe services with configuration from <c>DainnStripe</c> and optional overrides.
    /// </summary>
    public static IServiceCollection AddDainnStripe(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DainnStripeOptions> configureOptions)
    {
        var options = new DainnStripeOptions();
        configuration.GetSection("DainnStripe").Bind(options);
        configureOptions(options);

        ValidateConfiguration(configuration, options);

        StripeConfiguration.ApiKey = options.SecretKey;

        services.TryAddSingleton(options);
        services.TryAddSingleton<IOptions<DainnStripeOptions>>(Options.Create(options));
        services.TryAddSingleton<IDainnStripeClientFactory, DainnStripeClientFactory>();

        services.AddDainnStripeDbContext(configuration);
        services.TryAddScoped<IDainnStripeRequestContextAccessor, DainnStripeRequestContextAccessor>();
        services.TryAddScoped<IDainnStripeRequestOptionsFactory, DainnStripeRequestOptionsFactory>();
        services.TryAddScoped<IDainnStripeCatalogService, DainnStripeCatalogService>();
        services.TryAddScoped<IDainnStripeConnectAccountClient, DainnStripeConnectAccountClient>();
        services.TryAddScoped<IDainnStripeConnectService, DainnStripeConnectService>();
        services.TryAddScoped<IDainnStripeCheckoutSessionClient, DainnStripeCheckoutSessionClient>();
        services.TryAddScoped<IDainnStripeCheckoutService, DainnStripeCheckoutService>();
        services.TryAddScoped<IDainnStripePaymentIntentClient, DainnStripePaymentIntentClient>();
        services.TryAddScoped<IDainnStripePaymentService, DainnStripePaymentService>();
        services.TryAddScoped<IDainnStripeTransferClient, DainnStripeTransferClient>();
        services.TryAddScoped<IDainnStripePayoutClient, DainnStripePayoutClient>();
        services.TryAddScoped<IDainnStripeBalanceClient, DainnStripeBalanceClient>();
        services.TryAddScoped<IDainnStripeMoneyMovementService, DainnStripeMoneyMovementService>();
        services.TryAddScoped<IDainnStripeReconciliationService, DainnStripeReconciliationService>();
        services.TryAddScoped<IDainnStripeSubscriptionService, DainnStripeSubscriptionService>();
        services.TryAddScoped<IStripeWebhookIdempotencyService, StripeWebhookIdempotencyService>();
        services.TryAddScoped<IStripeWebhookService, StripeWebhookService>();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStripeWebhookHandler, DainnStripePaymentWebhookHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStripeWebhookHandler, DainnStripeConnectAccountWebhookHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStripeWebhookHandler, DainnStripePayoutWebhookHandler>());
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IStripeWebhookHandler, DainnStripeBalanceWebhookHandler>());

        return services;
    }

    /// <summary>
    /// Adds DainnStripe middleware hooks. Place before mapped endpoints.
    /// </summary>
    public static IApplicationBuilder UseDainnStripe(this IApplicationBuilder app)
    {
        return app.UseMiddleware<DainnStripeRequestLoggingMiddleware>();
    }

    /// <summary>
    /// Maps the configured Stripe webhook endpoint.
    /// </summary>
    public static IEndpointRouteBuilder MapDainnStripeWebhooks(
        this IEndpointRouteBuilder endpoints,
        string? pattern = null)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<DainnStripeOptions>();
        endpoints.MapPost(pattern ?? options.WebhookPath, async (
            HttpRequest request,
            IStripeWebhookService webhookService,
            CancellationToken cancellationToken) =>
        {
            using var reader = new StreamReader(request.Body);
            var payload = await reader.ReadToEndAsync(cancellationToken);
            var signature = request.Headers["Stripe-Signature"].ToString();

            var result = await webhookService.ProcessAsync(payload, signature, cancellationToken);

            return Results.Ok(new
            {
                received = true,
                duplicate = result.IsDuplicate,
                eventId = result.EventRecord.StripeEventId,
                eventType = result.EventRecord.EventType,
                handlers = result.HandlerCount
            });
        }).AllowAnonymous();

        return endpoints;
    }

    /// <summary>
    /// Maps Connect tenant and connected account endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapDainnStripeConnectEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/dainnstripe/connect",
        bool requireAuthorization = true)
    {
        var group = endpoints.MapGroup(pattern);
        if (requireAuthorization)
        {
            group.RequireAuthorization();
        }

        group.MapPost("/tenants", async (
            UpsertTenantRequest request,
            IDainnStripeConnectService connectService,
            CancellationToken cancellationToken) =>
        {
            var tenant = await connectService.UpsertTenantAsync(request, cancellationToken);
            return Results.Ok(tenant);
        });

        group.MapPost("/accounts", async (
            CreateConnectedAccountRequest request,
            IDainnStripeConnectService connectService,
            CancellationToken cancellationToken) =>
        {
            var account = await connectService.CreateConnectedAccountAsync(request, cancellationToken);
            return Results.Ok(account);
        });

        group.MapPost("/account-links", async (
            CreateConnectedAccountLinkRequest request,
            IDainnStripeConnectService connectService,
            CancellationToken cancellationToken) =>
        {
            var link = await connectService.CreateOnboardingLinkAsync(request, cancellationToken);
            return Results.Ok(link);
        });

        return endpoints;
    }

    /// <summary>
    /// Maps marketplace money movement endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapDainnStripeMoneyMovementEndpoints(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/dainnstripe/money-movement",
        bool requireAuthorization = true)
    {
        var group = endpoints.MapGroup(pattern);
        if (requireAuthorization)
        {
            group.RequireAuthorization();
        }

        group.MapPost("/transfers", async (
            CreateTransferRequest request,
            IDainnStripeMoneyMovementService moneyMovementService,
            CancellationToken cancellationToken) =>
        {
            var transfer = await moneyMovementService.CreateTransferAsync(request, cancellationToken);
            return Results.Ok(transfer);
        });

        group.MapPost("/payouts", async (
            CreatePayoutRequest request,
            IDainnStripeMoneyMovementService moneyMovementService,
            CancellationToken cancellationToken) =>
        {
            var payout = await moneyMovementService.CreatePayoutAsync(request, cancellationToken);
            return Results.Ok(payout);
        });

        group.MapPost("/reconcile", async (
            ReconcileMoneyMovementRequest request,
            IDainnStripeReconciliationService reconciliationService,
            CancellationToken cancellationToken) =>
        {
            var result = await reconciliationService.ReconcileMoneyMovementAsync(request, cancellationToken);
            return Results.Ok(result);
        });

        return endpoints;
    }

    private static IServiceCollection AddDainnStripeDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["DainnStripe:Database:Provider"] ?? "SqlServer";
        var connectionString = configuration["DainnStripe:Database:ConnectionString"]
            ?? throw new InvalidOperationException("DainnStripe database connection string is not configured.");

        services.AddDbContext<DainnStripeDbContext>(options =>
        {
            switch (provider)
            {
                case "SqlServer":
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(DainnStripeDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    });
                    break;

                case "PostgreSQL":
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(DainnStripeDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    });
                    break;

                case "MySQL":
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                    {
                        mySqlOptions.MigrationsAssembly(typeof(DainnStripeDbContext).Assembly.FullName);
                        mySqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    });
                    break;

                case "SQLite":
                    options.UseSqlite(connectionString, sqliteOptions =>
                    {
                        sqliteOptions.MigrationsAssembly(typeof(DainnStripeDbContext).Assembly.FullName);
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported DainnStripe database provider: {provider}");
            }
        });

        return services;
    }

    private static void ValidateConfiguration(IConfiguration configuration, DainnStripeOptions options)
    {
        if (!options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new InvalidOperationException(
                "DainnStripe secret key is not configured. Set 'DainnStripe:SecretKey'.");
        }

        if (string.IsNullOrWhiteSpace(options.WebhookSigningSecret))
        {
            throw new InvalidOperationException(
                "DainnStripe webhook signing secret is not configured. Set 'DainnStripe:WebhookSigningSecret'.");
        }

        var provider = configuration["DainnStripe:Database:Provider"];
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException(
                "DainnStripe database provider is not configured. Set 'DainnStripe:Database:Provider'.");
        }

        var connectionString = configuration["DainnStripe:Database:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "DainnStripe database connection string is not configured. Set 'DainnStripe:Database:ConnectionString'.");
        }
    }
}
