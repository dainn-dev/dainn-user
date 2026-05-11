using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Models.Profile;
using DainnUser.Infrastructure.Repositories;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.IntegrationTests.Services;

public class ProfileServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly ProfileService _profileService;
    private readonly UserRepository _userRepository;
    private readonly UnitOfWork _unitOfWork;

    public ProfileServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase();

        _userRepository = new UserRepository(_fixture.DbContext);
        _unitOfWork = new UnitOfWork(_fixture.DbContext);
        _profileService = new ProfileService(_userRepository, _unitOfWork);
    }

    [Fact]
    public async Task UpdateProfileAsync_CreatesProfileAndReadItBack()
    {
        // Arrange
        var user = await CreateUserAsync("profile1@example.com", "profileuser1");
        var updateDto = new UpdateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "Johnny",
            DateOfBirth = new DateTime(1990, 5, 15),
            Gender = "Male",
            Language = "en",
            Timezone = "America/New_York",
            Bio = "Software developer",
            Website = "https://johndoe.com"
        };

        // Act - Create profile
        var updateResult = await _profileService.UpdateProfileAsync(user.Id, updateDto);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - Update result
        updateResult.Should().NotBeNull();
        updateResult.UserId.Should().Be(user.Id);
        updateResult.FirstName.Should().Be("John");
        updateResult.LastName.Should().Be("Doe");
        updateResult.DisplayName.Should().Be("Johnny");
        updateResult.DateOfBirth.Should().Be(new DateTime(1990, 5, 15));
        updateResult.Gender.Should().Be("Male");
        updateResult.Language.Should().Be("en");
        updateResult.Timezone.Should().Be("America/New_York");
        updateResult.Bio.Should().Be("Software developer");
        updateResult.Website.Should().Be("https://johndoe.com");

        // Act - Read profile back
        var getResult = await _profileService.GetProfileAsync(user.Id);

        // Assert - Get result matches
        getResult.Should().NotBeNull();
        getResult.UserId.Should().Be(user.Id);
        getResult.Email.Should().Be("profile1@example.com");
        getResult.Username.Should().Be("profileuser1");
        getResult.FirstName.Should().Be("John");
        getResult.LastName.Should().Be("Doe");
        getResult.DisplayName.Should().Be("Johnny");
        getResult.DateOfBirth.Should().Be(new DateTime(1990, 5, 15));
        getResult.Gender.Should().Be("Male");
        getResult.Language.Should().Be("en");
        getResult.Timezone.Should().Be("America/New_York");
        getResult.Bio.Should().Be("Software developer");
        getResult.Website.Should().Be("https://johndoe.com");
        getResult.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        getResult.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify profile exists in database
        var profileInDb = await _fixture.DbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == user.Id);
        profileInDb.Should().NotBeNull();
        profileInDb!.FirstName.Should().Be("John");
        profileInDb.LastName.Should().Be("Doe");
    }

    [Fact]
    public async Task UpdateProfileAsync_UpdatesExistingProfileFields()
    {
        // Arrange - Create user with initial profile
        var user = await CreateUserAsync("profile2@example.com", "profileuser2");
        var initialDto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Gender = "Female",
            Language = "en",
            Bio = "Initial bio"
        };
        await _profileService.UpdateProfileAsync(user.Id, initialDto);
        _fixture.DbContext.ChangeTracker.Clear();

        var profileBeforeUpdate = await _fixture.DbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(p => p.UserId == user.Id);
        var createdAt = profileBeforeUpdate.CreatedAt;
        // FirstName + LastName with no explicit DisplayName → fallback is applied
        profileBeforeUpdate.DisplayName.Should().Be("Jane Smith");

        // Act - Update some fields
        var updateDto = new UpdateProfileDto
        {
            FirstName = "Janet",
            Bio = "Updated bio",
            Website = "https://janet.com"
        };
        var updateResult = await _profileService.UpdateProfileAsync(user.Id, updateDto);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - Updated fields changed, others preserved
        updateResult.FirstName.Should().Be("Janet");
        updateResult.LastName.Should().Be("Smith");
        // Existing derived DisplayName is preserved unless explicitly cleared.
        updateResult.DisplayName.Should().Be("Jane Smith");
        updateResult.Gender.Should().Be("Female");
        updateResult.Language.Should().Be("en");
        updateResult.Bio.Should().Be("Updated bio");
        updateResult.Website.Should().Be("https://janet.com");

        // Verify in database
        var profileInDb = await _fixture.DbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(p => p.UserId == user.Id);
        profileInDb.FirstName.Should().Be("Janet");
        profileInDb.LastName.Should().Be("Smith");
        profileInDb.DisplayName.Should().Be("Jane Smith");
        profileInDb.Gender.Should().Be("Female");
        profileInDb.Bio.Should().Be("Updated bio");
        profileInDb.Website.Should().Be("https://janet.com");
        profileInDb.CreatedAt.Should().Be(createdAt);
        profileInDb.UpdatedAt.Should().BeAfter(createdAt);
    }

    [Fact]
    public async Task GetProfileAsync_WhenNoProfile_ReturnsUserDataWithNullProfileFields()
    {
        // Arrange
        var user = await CreateUserAsync("noprofile@example.com", "noprofileuser");
        _fixture.DbContext.ChangeTracker.Clear();

        // Act
        var result = await _profileService.GetProfileAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(user.Id);
        result.Email.Should().Be("noprofile@example.com");
        result.Username.Should().Be("noprofileuser");
        result.FirstName.Should().BeNull();
        result.LastName.Should().BeNull();
        result.DisplayName.Should().BeNull();
        result.AvatarUrl.Should().BeNull();
        result.DateOfBirth.Should().BeNull();
        result.Gender.Should().BeNull();
        result.Language.Should().BeNull();
        result.Timezone.Should().BeNull();
        result.Bio.Should().BeNull();
        result.Website.Should().BeNull();
        result.CreatedAt.Should().Be(user.CreatedAt);
        result.UpdatedAt.Should().Be(user.UpdatedAt);
    }

    [Fact]
    public async Task UpdateProfileAsync_DisplayNameFallback_WorksEndToEnd()
    {
        // Arrange
        var user = await CreateUserAsync("fallback@example.com", "fallbackuser");
        var dto = new UpdateProfileDto
        {
            FirstName = "Alice",
            LastName = "Wonder",
            DisplayName = null
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(user.Id, dto);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert
        result.DisplayName.Should().Be("Alice Wonder");

        var profileInDb = await _fixture.DbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(p => p.UserId == user.Id);
        profileInDb.DisplayName.Should().Be("Alice Wonder");
    }

    [Fact]
    public async Task UpdateProfileAsync_StringNormalization_WorksEndToEnd()
    {
        // Arrange
        var user = await CreateUserAsync("normalize@example.com", "normalizeuser");
        var dto = new UpdateProfileDto
        {
            FirstName = "  Trimmed  ",
            LastName = "   ",
            Gender = "  NonBinary  ",
            Language = "",
            Timezone = "  Asia/Tokyo  ",
            Bio = "  My story  ",
            Website = "     "
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(user.Id, dto);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert
        result.FirstName.Should().Be("Trimmed");
        result.LastName.Should().BeNull();
        result.DisplayName.Should().Be("Trimmed");
        result.Gender.Should().Be("NonBinary");
        result.Language.Should().BeNull();
        result.Timezone.Should().Be("Asia/Tokyo");
        result.Bio.Should().Be("My story");
        result.Website.Should().BeNull();

        var profileInDb = await _fixture.DbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(p => p.UserId == user.Id);
        profileInDb.FirstName.Should().Be("Trimmed");
        profileInDb.LastName.Should().BeNull();
        profileInDb.Gender.Should().Be("NonBinary");
        profileInDb.Timezone.Should().Be("Asia/Tokyo");
        profileInDb.Bio.Should().Be("My story");
    }

    [Fact]
    public async Task UpdateProfileAsync_MultipleUpdates_PreservesCreatedAt()
    {
        // Arrange
        var user = await CreateUserAsync("multiple@example.com", "multipleuser");

        // Act - First update
        var dto1 = new UpdateProfileDto { FirstName = "First" };
        await _profileService.UpdateProfileAsync(user.Id, dto1);
        _fixture.DbContext.ChangeTracker.Clear();

        var afterFirst = await _fixture.DbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(p => p.UserId == user.Id);
        var createdAt = afterFirst.CreatedAt;
        var updatedAt1 = afterFirst.UpdatedAt;

        // Small delay to ensure UpdatedAt changes
        await Task.Delay(100);

        // Act - Second update
        var dto2 = new UpdateProfileDto { LastName = "Second" };
        await _profileService.UpdateProfileAsync(user.Id, dto2);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert
        var afterSecond = await _fixture.DbContext.UserProfiles
            .AsNoTracking()
            .FirstAsync(p => p.UserId == user.Id);
        afterSecond.CreatedAt.Should().Be(createdAt);
        afterSecond.UpdatedAt.Should().BeAfter(updatedAt1);
        afterSecond.FirstName.Should().Be("First");
        afterSecond.LastName.Should().Be("Second");
    }

    private async Task<User> CreateUserAsync(string email, string username)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            Status = UserStatus.Active,
            EmailVerified = true,
            PasswordHash = "dummy-hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();
        return user;
    }
}
