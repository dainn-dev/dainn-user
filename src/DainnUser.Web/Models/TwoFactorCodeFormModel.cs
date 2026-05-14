using System.ComponentModel.DataAnnotations;

namespace DainnUser.Web.Models;

/// <summary>
/// Model used by the two-factor code form component.
/// </summary>
public class TwoFactorCodeFormModel
{
    [Required(ErrorMessage = "Code is required.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Enter the 6-digit code.")]
    public string Code { get; set; } = string.Empty;

    public bool RememberDevice { get; set; }
}
