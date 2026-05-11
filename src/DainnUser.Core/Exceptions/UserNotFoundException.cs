namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when a user cannot be found by the provided identifier.
/// </summary>
public class UserNotFoundException : Exception
{
    /// <summary>
    /// Gets the user identifier that could not be found.
    /// </summary>
    public Guid UserId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotFoundException"/> class.
    /// </summary>
    /// <param name="userId">The user identifier that could not be found.</param>
    public UserNotFoundException(Guid userId)
        : base($"User with id '{userId}' was not found.")
    {
        UserId = userId;
    }
}
