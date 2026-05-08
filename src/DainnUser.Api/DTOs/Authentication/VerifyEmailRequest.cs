using System.ComponentModel.DataAnnotations;

namespace DainnUser.Api.DTOs.Authentication;

/// <summary>
/// Request DTO for email verification.
/// </summary>
public class VerifyEmailRequest
{
    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    [Required(ErrorMessage = "User ID is required.")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the verification token.
    /// </summary>
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;
}
