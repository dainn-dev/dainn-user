using DainnStripe.Entities;
using DainnStripe.Models;

namespace DainnStripe.Interfaces;

/// <summary>
/// Manages SaaS tenants and Stripe connected accounts.
/// </summary>
public interface IDainnStripeConnectService
{
    /// <summary>
    /// Creates or updates a tenant.
    /// </summary>
    Task<DainnStripeTenant> UpsertTenantAsync(
        UpsertTenantRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a connected account for a tenant owner.
    /// </summary>
    Task<DainnStripeConnectedAccount> CreateConnectedAccountAsync(
        CreateConnectedAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an onboarding link and stores it on the local account.
    /// </summary>
    Task<ConnectedAccountLinkResult> CreateOnboardingLinkAsync(
        CreateConnectedAccountLinkRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs local connected account status from Stripe data.
    /// </summary>
    Task<DainnStripeConnectedAccount?> SyncConnectedAccountAsync(
        SyncConnectedAccountRequest request,
        CancellationToken cancellationToken = default);
}
