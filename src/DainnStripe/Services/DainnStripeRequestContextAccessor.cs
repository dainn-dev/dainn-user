using DainnStripe.Interfaces;
using DainnStripe.Models;

namespace DainnStripe.Services;

/// <summary>
/// Default scoped Stripe request context accessor.
/// </summary>
public class DainnStripeRequestContextAccessor : IDainnStripeRequestContextAccessor
{
    /// <inheritdoc />
    public DainnStripeRequestContext Current { get; } = new();

    /// <inheritdoc />
    public void SetConnectedAccount(string stripeAccountId, string? tenantId = null)
    {
        if (string.IsNullOrWhiteSpace(stripeAccountId))
        {
            throw new ArgumentException("Value is required.", nameof(stripeAccountId));
        }

        Current.StripeAccountId = stripeAccountId;
        Current.TenantId = tenantId;
    }

    /// <inheritdoc />
    public void Clear()
    {
        Current.StripeAccountId = null;
        Current.TenantId = null;
    }
}
