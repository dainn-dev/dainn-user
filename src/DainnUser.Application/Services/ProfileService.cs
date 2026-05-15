using DainnUser.Core.Entities;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Profile;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for profile management.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public ProfileService(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetWithProfileAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(userId);
        }

        return MapToDto(user);
    }

    /// <inheritdoc/>
    public async Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var user = await _userRepository.GetWithProfileAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new UserNotFoundException(userId);
        }

        var now = DateTime.UtcNow;
        var profile = user.Profile;
        if (profile is null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                CreatedAt = now
            };

            user.Profile = profile;
            await _userRepository.AddProfileAsync(profile, cancellationToken);
        }

        ApplyUpdates(profile, dto);

        if (string.IsNullOrWhiteSpace(profile.DisplayName))
        {
            profile.DisplayName = DeriveDisplayName(profile.FirstName, profile.LastName);
        }

        profile.UpdatedAt = now;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(user);
    }

    private static void ApplyUpdates(UserProfile profile, UpdateProfileDto dto)
    {
        if (dto.FirstName is not null)
        {
            profile.FirstName = Normalize(dto.FirstName);
        }

        if (dto.LastName is not null)
        {
            profile.LastName = Normalize(dto.LastName);
        }

        if (dto.DisplayName is not null)
        {
            profile.DisplayName = Normalize(dto.DisplayName);
        }

        if (dto.DateOfBirth.HasValue)
        {
            profile.DateOfBirth = dto.DateOfBirth;
        }

        if (dto.Gender is not null)
        {
            profile.Gender = Normalize(dto.Gender);
        }

        if (dto.Language is not null)
        {
            profile.Language = Normalize(dto.Language);
        }

        if (dto.Timezone is not null)
        {
            profile.Timezone = Normalize(dto.Timezone);
        }

        if (dto.Bio is not null)
        {
            profile.Bio = Normalize(dto.Bio);
        }

        if (dto.Website is not null)
        {
            profile.Website = Normalize(dto.Website);
        }

        if (dto.AvatarUrl is not null)
        {
            if (!Uri.TryCreate(dto.AvatarUrl, UriKind.Absolute, out var avatarUri)
                || (avatarUri.Scheme != Uri.UriSchemeHttps && avatarUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new ArgumentException("AvatarUrl must be a valid http or https URL.", nameof(dto));
            }

            profile.AvatarUrl = dto.AvatarUrl;
        }
    }

    private static ProfileDto MapToDto(User user)
    {
        var profile = user.Profile;

        return new ProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            Username = user.Username,
            FirstName = profile?.FirstName,
            LastName = profile?.LastName,
            DisplayName = profile?.DisplayName,
            AvatarUrl = profile?.AvatarUrl,
            DateOfBirth = profile?.DateOfBirth,
            Gender = profile?.Gender,
            Language = profile?.Language,
            Timezone = profile?.Timezone,
            Bio = profile?.Bio,
            Website = profile?.Website,
            CreatedAt = profile?.CreatedAt ?? user.CreatedAt,
            UpdatedAt = profile?.UpdatedAt ?? user.UpdatedAt
        };
    }

    private static string? Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? DeriveDisplayName(string? firstName, string? lastName)
    {
        var parts = new[] { firstName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim());

        var displayName = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(displayName) ? null : displayName;
    }
}