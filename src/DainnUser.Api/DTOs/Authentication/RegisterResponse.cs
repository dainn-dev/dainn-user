namespace DainnUser.Api.DTOs.Authentication;

/// <summary>
/// Response DTO for user registration.
/// </summary>
public class RegisterResponse
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the response message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
