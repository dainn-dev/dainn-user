using System.ComponentModel.DataAnnotations;

namespace DainnUser.Web.Models;

/// <summary>
/// Model used by the profile form component.
/// </summary>
public class ProfileFormModel
{
    [StringLength(100, ErrorMessage = "First name must not exceed 100 characters.")]
    public string? FirstName { get; set; }

    [StringLength(100, ErrorMessage = "Last name must not exceed 100 characters.")]
    public string? LastName { get; set; }

    [StringLength(20, ErrorMessage = "Phone number must not exceed 20 characters.")]
    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "Bio must not exceed 500 characters.")]
    public string? Bio { get; set; }

    [Url(ErrorMessage = "Website must be a valid URL.")]
    [StringLength(500, ErrorMessage = "Website must not exceed 500 characters.")]
    public string? Website { get; set; }
}
