namespace DainnStripe.Exceptions;

/// <summary>
/// Thrown when a Stripe webhook event is received with the same event ID as a previously stored record
/// but with a different payload fingerprint, indicating a replay attack or data integrity issue.
/// </summary>
public class StripeWebhookFingerprintConflictException : Exception
{
    /// <summary>
    /// Gets the Stripe event identifier that caused the conflict.
    /// </summary>
    public string StripeEventId { get; }

    /// <summary>
    /// Gets the first 8 hex characters of the stored payload hash.
    /// </summary>
    public string StoredHashPrefix { get; }

    /// <summary>
    /// Gets the first 8 hex characters of the incoming payload hash.
    /// </summary>
    public string IncomingHashPrefix { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="StripeWebhookFingerprintConflictException"/>.
    /// </summary>
    public StripeWebhookFingerprintConflictException(
        string stripeEventId,
        string storedHash,
        string incomingHash)
        : base(
            $"Webhook fingerprint conflict for event '{stripeEventId}'. " +
            $"Payload does not match the stored record. " +
            $"Stored: {storedHash[..Math.Min(8, storedHash.Length)]}… " +
            $"Incoming: {incomingHash[..Math.Min(8, incomingHash.Length)]}…")
    {
        StripeEventId = stripeEventId;
        StoredHashPrefix = storedHash[..Math.Min(8, storedHash.Length)];
        IncomingHashPrefix = incomingHash[..Math.Min(8, incomingHash.Length)];
    }
}
