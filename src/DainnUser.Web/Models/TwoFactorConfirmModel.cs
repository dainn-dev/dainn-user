using System.ComponentModel.DataAnnotations;

namespace DainnUser.Web.Models;

/// <summary>
/// Model for 2FA confirmation actions (enable, disable, regenerate backup codes).
/// </summary>
public class TwoFactorConfirmModel
{
    [Required(ErrorMessage = "Authentication code is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter the 6-digit code from your authenticator app.")]
    public string Code { get; set; } = string.Empty;
}
