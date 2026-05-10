namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when login credentials cannot be verified. Intentionally generic to avoid user enumeration.
/// </summary>
public class InvalidCredentialsException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class.
    /// </summary>
    public InvalidCredentialsException()
        : base("Invalid email or password.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCredentialsException"/> class with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidCredentialsException(string message) : base(message)
    {
    }
}
