using System.ComponentModel.DataAnnotations;

namespace DainnUser.Web.Models;

/// <summary>
/// Model used by the login form component.
/// </summary>
public class LoginFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    public string? RememberDeviceToken { get; set; }
}
