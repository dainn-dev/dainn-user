namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when login is attempted on an account whose email has not been verified
/// while email verification is required.
/// </summary>
public class EmailNotVerifiedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EmailNotVerifiedException"/> class.
    /// </summary>
    public EmailNotVerifiedException()
        : base("Email address has not been verified.")
    {
    }
}
