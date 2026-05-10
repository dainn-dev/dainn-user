namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when a refresh token cannot be honored — unknown, expired, revoked, or replayed.
/// Intentionally generic to avoid leaking which condition failed.
/// </summary>
public class InvalidRefreshTokenException : Exception
{
    /// <summary>
    /// Gets a value indicating whether this failure was caused by reuse of an already-consumed token
    /// (a strong indicator of theft). When true, callers should treat this as a security incident.
    /// </summary>
    public bool IsReuseDetected { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRefreshTokenException"/> class.
    /// </summary>
    public InvalidRefreshTokenException() : this("Invalid refresh token.", isReuseDetected: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidRefreshTokenException"/> class.
    /// </summary>
    /// <param name="message">Error message.</param>
    /// <param name="isReuseDetected">True if the failure is due to reuse of a consumed token.</param>
    public InvalidRefreshTokenException(string message, bool isReuseDetected = false) : base(message)
    {
        IsReuseDetected = isReuseDetected;
    }
}
