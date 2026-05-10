namespace DainnUser.Api.DTOs.Authentication;

/// <summary>
/// API response containing newly generated two-factor backup codes.
/// </summary>
public class BackupCodesResponse
{
    /// <summary>
    /// Gets or sets the one-time backup codes.
    /// </summary>
    public IReadOnlyList<string> BackupCodes { get; set; } = Array.Empty<string>();
}
