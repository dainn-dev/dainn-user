using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Stripe Connect account client abstraction.
/// </summary>
public interface IDainnStripeConnectAccountClient
{
    /// <summary>
    /// Creates a Stripe connected account.
    /// </summary>
    Task<ConnectedAccountResult> CreateAccountAsync(
        CreateConnectedAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a Stripe onboarding account link.
    /// </summary>
    Task<ConnectedAccountLinkResult> CreateAccountLinkAsync(
        CreateConnectedAccountLinkRequest request,
        CancellationToken cancellationToken = default);
}
