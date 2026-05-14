namespace DainnStripe.Enums;

/// <summary>
/// Local status for a Stripe connected account.
/// </summary>
public enum DainnStripeConnectedAccountStatus
{
    /// <summary>
    /// Account has not completed onboarding.
    /// </summary>
    OnboardingRequired = 0,

    /// <summary>
    /// Account details are submitted but Stripe still restricts some capability.
    /// </summary>
    Restricted = 1,

    /// <summary>
    /// Account can accept charges and receive payouts.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Account is disabled locally.
    /// </summary>
    Disabled = 3
}
