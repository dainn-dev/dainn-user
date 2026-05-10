namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when a user provides an incorrect current password during a change-password operation.
/// </summary>
public class InvalidCurrentPasswordException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCurrentPasswordException"/> class.
    /// </summary>
    public InvalidCurrentPasswordException()
        : base("The current password provided is incorrect.")
    {
    }
}
