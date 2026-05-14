using DainnStripe.Interfaces;
using DainnStripe.Models;
using Stripe;

namespace DainnStripe.Services;

/// <summary>
/// Stripe.net backed Connect account client.
/// </summary>
public class DainnStripeConnectAccountClient : IDainnStripeConnectAccountClient
{
    private readonly AccountService _accountService;
    private readonly AccountLinkService _accountLinkService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeConnectAccountClient"/> class.
    /// </summary>
    public DainnStripeConnectAccountClient()
        : this(new AccountService(), new AccountLinkService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeConnectAccountClient"/> class.
    /// </summary>
    public DainnStripeConnectAccountClient(
        AccountService accountService,
        AccountLinkService accountLinkService)
    {
        _accountService = accountService;
        _accountLinkService = accountLinkService;
    }

    /// <inheritdoc />
    public async Task<ConnectedAccountResult> CreateAccountAsync(
        CreateConnectedAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new AccountCreateOptions
        {
            Type = "express",
            Country = request.Country,
            Email = request.Email,
            DefaultCurrency = request.DefaultCurrency,
            Metadata = request.Metadata.Count == 0
                ? null
                : new Dictionary<string, string>(request.Metadata, StringComparer.Ordinal)
        };

        var account = await _accountService.CreateAsync(options, requestOptions: null, cancellationToken);

        return new ConnectedAccountResult
        {
            StripeAccountId = account.Id,
            Email = account.Email,
            Country = account.Country,
            DefaultCurrency = account.DefaultCurrency,
            ChargesEnabled = account.ChargesEnabled,
            PayoutsEnabled = account.PayoutsEnabled,
            DetailsSubmitted = account.DetailsSubmitted
        };
    }

    /// <inheritdoc />
    public async Task<ConnectedAccountLinkResult> CreateAccountLinkAsync(
        CreateConnectedAccountLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var options = new AccountLinkCreateOptions
        {
            Account = request.StripeAccountId,
            ReturnUrl = request.ReturnUrl,
            RefreshUrl = request.RefreshUrl,
            Type = "account_onboarding"
        };

        var accountLink = await _accountLinkService.CreateAsync(options, requestOptions: null, cancellationToken);

        return new ConnectedAccountLinkResult
        {
            Url = accountLink.Url,
            ExpiresAt = accountLink.ExpiresAt
        };
    }
}
