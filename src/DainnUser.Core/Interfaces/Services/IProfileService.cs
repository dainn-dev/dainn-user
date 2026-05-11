using DainnUser.Core.Models.Profile;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for managing user profiles.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gets the profile for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's profile.</returns>
    Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the profile for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="dto">The profile update data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated profile.</returns>
    Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
}