using System.ComponentModel.DataAnnotations;

namespace DainnUser.Api.DTOs.Authentication;

/// <summary>
/// Request DTO for resending verification email.
/// </summary>
public class ResendVerificationRequest
{
    /// <summary>
    /// Gets or sets the email address.
    /// </summary>
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;
}
