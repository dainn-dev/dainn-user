namespace DainnUser.Core.Exceptions;

/// <summary>
/// Exception thrown when a contact verification code is invalid or expired.
/// </summary>
public class InvalidVerificationCodeException : Exception
{
    public InvalidVerificationCodeException()
        : base("The verification code is invalid or has expired.")
    {
    }
}
