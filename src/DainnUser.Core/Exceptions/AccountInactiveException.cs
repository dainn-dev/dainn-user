using DainnUser.Core.Enums;

namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when login is attempted on an account whose status prevents authentication
/// (e.g., suspended or deactivated).
/// </summary>
public class AccountInactiveException : Exception
{
    /// <summary>
    /// Gets the user status that caused the failure.
    /// </summary>
    public UserStatus Status { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountInactiveException"/> class.
    /// </summary>
    /// <param name="status">The user status that caused the failure.</param>
    public AccountInactiveException(UserStatus status)
        : base($"Account is not active (status: {status}).")
    {
        Status = status;
    }
}
