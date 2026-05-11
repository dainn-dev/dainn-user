# PSA-65 Profile Management Design

## Goal

Implement profile management for DainnUser so consumers can retrieve and update extended user profile data through an application service. The feature must use the existing `UserProfile` entity and follow the library's service, DTO, validation, repository, and test patterns.

## Scope

In scope:

- Add `IProfileService` with get and update methods.
- Add profile DTOs for read and update operations.
- Add FluentValidation rules for profile updates.
- Add `ProfileService` implementation.
- Register the service in dependency injection.
- Add unit and integration tests.

Out of scope:

- Profile API controller endpoints; covered by PSA-84.
- Avatar upload/storage; covered by PSA-66.
- Address/contact management; covered by PSA-77 and PSA-78.

## Existing Context

The codebase already has:

- `UserProfile` entity with extended fields.
- `User.Profile` navigation property.
- `DainnUserDbContext.UserProfiles` DbSet.
- EF Core configuration for `UserProfile`.
- `IUserRepository.GetWithProfileAsync(Guid userId, CancellationToken)`.

This feature should build on those pieces rather than introduce a separate profile repository unless implementation proves the existing repository insufficient.

## Service Contract

Create `IProfileService` in `DainnUser.Core.Interfaces.Services`:

```csharp
Task<ProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
Task<ProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);
```

Behavior:

- `GetProfileAsync` loads the user and profile.
- If the user does not exist, throw `InvalidOperationException` with a clear message.
- If the user exists but has no profile row, return a `ProfileDto` from user data with nullable profile fields left null. Do not create a database row during get.
- `UpdateProfileAsync` creates a `UserProfile` row if missing, otherwise updates the existing profile.
- `UpdateProfileAsync` persists via `IUnitOfWork.SaveChangesAsync`.

## DTOs

Create profile DTOs under `DainnUser.Application.DTOs.Profile`.

`ProfileDto` includes:

- `UserId`
- `Email`
- `Username`
- `FirstName`
- `LastName`
- `DisplayName`
- `AvatarUrl`
- `DateOfBirth`
- `Gender`
- `Language`
- `Timezone`
- `Bio`
- `Website`
- `CreatedAt`
- `UpdatedAt`

`UpdateProfileDto` includes:

- `FirstName`
- `LastName`
- `DisplayName`
- `DateOfBirth`
- `Gender`
- `Language`
- `Timezone`
- `Bio`
- `Website`

`AvatarUrl` is intentionally not updateable here because avatar upload and storage are handled by PSA-66.

## Validation

Create `UpdateProfileDtoValidator` with these rules:

- `FirstName`, `LastName`, `DisplayName`: max 100 characters.
- `Bio`: max 500 characters.
- `DateOfBirth`: must not be in the future.
- `Language`: when present, must be a two-letter ISO 639-1 language code format.
- `Timezone`: when present, must resolve through `TimeZoneInfo.FindSystemTimeZoneById`.
- `Website`: when present, must be an absolute HTTP/HTTPS URL.

Validation belongs in the application layer and should be usable independently in tests.

## Mapping and Normalization

`ProfileService` should normalize user input before saving:

- Trim string fields.
- Store whitespace-only strings as null.
- If `DisplayName` is null/empty and either first or last name exists, derive it from `FirstName` + `LastName`.
- Use UTC timestamps.
- Preserve `CreatedAt` on existing profile rows.
- Update `UpdatedAt` on every successful update.

## Error Handling

- Missing user: throw `InvalidOperationException("User not found.")`.
- Validation is handled by FluentValidation before service calls where consumers wire validators. Service may still rely on normalized input and should not duplicate all validator logic.
- Database errors are not swallowed.

## Testing

Unit tests should cover:

- Get returns user-backed DTO when profile row is missing.
- Get returns full DTO when profile exists.
- Update creates profile when missing.
- Update modifies existing profile and preserves `CreatedAt`.
- Update trims strings and converts whitespace-only values to null.
- DisplayName fallback behavior.
- Validator rejects future date of birth, invalid language, invalid timezone, invalid URL, and over-length fields.

Integration tests should cover:

- Profile can be created and read through `ProfileService` using test database fixture.
- Updating an existing profile persists changes.

## Security and Compatibility

- No authorization decisions are implemented here; callers/controllers must ensure the user can access the requested profile.
- Input validation prevents unsafe or malformed profile values at the application boundary.
- No breaking changes to existing public service contracts.
- No in-memory state is introduced.
