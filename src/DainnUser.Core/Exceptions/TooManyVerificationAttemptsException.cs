namespace DainnUser.Core.Exceptions;

/// <summary>
/// Exception thrown when contact verification code requests exceed the allowed limit.
/// </summary>
public class TooManyVerificationAttemptsException : Exception
{
    public TooManyVerificationAttemptsException()
        : base("Too many verification attempts. Please try again later.")
    {
    }
}
