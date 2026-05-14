using System.ComponentModel.DataAnnotations;

namespace DainnUser.Web.Models;

/// <summary>
/// Model used by the forgot password form component.
/// </summary>
public class ForgotPasswordFormModel
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public string Email { get; set; } = string.Empty;
}
