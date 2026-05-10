namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when a password reset token cannot be honored — unknown, expired, revoked, or already used.
/// Intentionally generic to avoid leaking which condition failed.
/// </summary>
public class InvalidPasswordResetTokenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordResetTokenException"/> class.
    /// </summary>
    public InvalidPasswordResetTokenException()
        : base("Invalid or expired password reset token.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordResetTokenException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidPasswordResetTokenException(string message) : base(message)
    {
    }
}
