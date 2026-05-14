namespace DainnUser.Web.Models;

/// <summary>
/// Describes a social login provider rendered by SocialLoginButtons.
/// </summary>
public class SocialLoginProvider
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Url { get; set; }
}
