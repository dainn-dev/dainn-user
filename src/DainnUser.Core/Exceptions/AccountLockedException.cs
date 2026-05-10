namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when a login attempt is made against a locked-out account.
/// </summary>
public class AccountLockedException : Exception
{
    /// <summary>
    /// Gets the time at which the lockout will be released (UTC), if known.
    /// </summary>
    public DateTime? LockoutEnd { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountLockedException"/> class.
    /// </summary>
    /// <param name="lockoutEnd">The time at which the lockout will be released.</param>
    public AccountLockedException(DateTime? lockoutEnd)
        : base($"Account is locked due to too many failed login attempts. Try again after {lockoutEnd?.ToString("u") ?? "the lockout period"}.")
    {
        LockoutEnd = lockoutEnd;
    }
}
