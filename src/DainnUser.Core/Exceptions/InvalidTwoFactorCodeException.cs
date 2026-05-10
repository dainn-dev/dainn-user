namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when a provided two-factor authentication code is invalid, expired, or already used.
/// </summary>
public class InvalidTwoFactorCodeException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidTwoFactorCodeException"/> class.
    /// </summary>
    public InvalidTwoFactorCodeException()
        : base("The two-factor authentication code provided is invalid or expired.")
    {
    }
}
