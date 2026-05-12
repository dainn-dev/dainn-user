namespace DainnUser.Api.DTOs.Profile;

/// <summary>
/// API response wrapper for profile data.
/// </summary>
public class ProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Language { get; set; }
    public string? Timezone { get; set; }
    public string? Bio { get; set; }
    public string? Website { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
