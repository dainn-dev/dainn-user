using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Models.Profile;
using FluentAssertions;
using Moq;

namespace DainnUser.UnitTests.Services;

public class ProfileServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ProfileService _service;

    public ProfileServiceTests()
    {
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _service = new ProfileService(_userRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetProfileAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync((User?)null);

        var act = () => _service.GetProfileAsync(userId);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task GetProfileAsync_WhenProfileIsNull_ReturnsDtoFromUser()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Profile = null
        };
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);

        var result = await _service.GetProfileAsync(userId);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.Username.Should().Be("testuser");
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
    public async Task GetProfileAsync_WhenProfileExists_ReturnsFullDto()
    {
        var userId = Guid.NewGuid();
        var profileCreatedAt = DateTime.UtcNow.AddDays(-8);
        var profileUpdatedAt = DateTime.UtcNow.AddDays(-2);
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FirstName = "John",
                LastName = "Doe",
                DisplayName = "Johnny",
                AvatarUrl = "https://example.com/avatar.jpg",
                DateOfBirth = new DateTime(1990, 5, 15),
                Gender = "Male",
                Language = "en",
                Timezone = "America/New_York",
                Bio = "Software developer",
                Website = "https://johndoe.com",
                CreatedAt = profileCreatedAt,
                UpdatedAt = profileUpdatedAt
            }
        };
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);

        var result = await _service.GetProfileAsync(userId);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Email.Should().Be("test@example.com");
        result.Username.Should().Be("testuser");
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.DisplayName.Should().Be("Johnny");
        result.AvatarUrl.Should().Be("https://example.com/avatar.jpg");
        result.DateOfBirth.Should().Be(new DateTime(1990, 5, 15));
        result.Gender.Should().Be("Male");
        result.Language.Should().Be("en");
        result.Timezone.Should().Be("America/New_York");
        result.Bio.Should().Be("Software developer");
        result.Website.Should().Be("https://johndoe.com");
        result.CreatedAt.Should().Be(profileCreatedAt);
        result.UpdatedAt.Should().Be(profileUpdatedAt);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        var dto = new UpdateProfileDto { FirstName = "John" };
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync((User?)null);

        var act = () => _service.UpdateProfileAsync(userId, dto);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenProfileMissing_CreatesNewProfile()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            Profile = null
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith"
        };

        UserProfile? addedProfile = null;
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddProfileAsync(It.IsAny<UserProfile>(), default))
            .Callback<UserProfile, CancellationToken>((profile, _) => addedProfile = profile)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateProfileAsync(userId, dto);

        addedProfile.Should().NotBeNull();
        addedProfile!.UserId.Should().Be(userId);
        addedProfile.FirstName.Should().Be("Jane");
        addedProfile.LastName.Should().Be("Smith");
        addedProfile.DisplayName.Should().Be("Jane Smith");
        addedProfile.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        addedProfile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.DisplayName.Should().Be("Jane Smith");

        _userRepositoryMock.Verify(x => x.AddProfileAsync(It.IsAny<UserProfile>(), default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenProfileExists_UpdatesAndPreservesCreatedAt()
    {
        var userId = Guid.NewGuid();
        var profileCreatedAt = DateTime.UtcNow.AddDays(-30);
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Old",
            LastName = "Name",
            DisplayName = "OldDisplay",
            CreatedAt = profileCreatedAt,
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow.AddDays(-20),
            Profile = profile
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "New",
            LastName = "Name",
            DisplayName = "NewDisplay"
        };

        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);

        var result = await _service.UpdateProfileAsync(userId, dto);

        profile.FirstName.Should().Be("New");
        profile.LastName.Should().Be("Name");
        profile.DisplayName.Should().Be("NewDisplay");
        profile.CreatedAt.Should().Be(profileCreatedAt);
        profile.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("Name");
        result.DisplayName.Should().Be("NewDisplay");

        _userRepositoryMock.Verify(x => x.AddProfileAsync(It.IsAny<UserProfile>(), default), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_DisplayNameFallsBackToFirstNamePlusLastName()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = null
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "Alice",
            LastName = "Johnson",
            DisplayName = null
        };

        UserProfile? addedProfile = null;
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddProfileAsync(It.IsAny<UserProfile>(), default))
            .Callback<UserProfile, CancellationToken>((profile, _) => addedProfile = profile)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateProfileAsync(userId, dto);

        addedProfile!.DisplayName.Should().Be("Alice Johnson");
        result.DisplayName.Should().Be("Alice Johnson");
    }

    [Fact]
    public async Task UpdateProfileAsync_DisplayNameFallbackWithOnlyFirstName()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = null
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "Bob",
            LastName = null,
            DisplayName = null
        };

        UserProfile? addedProfile = null;
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddProfileAsync(It.IsAny<UserProfile>(), default))
            .Callback<UserProfile, CancellationToken>((profile, _) => addedProfile = profile)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateProfileAsync(userId, dto);

        addedProfile!.DisplayName.Should().Be("Bob");
        result.DisplayName.Should().Be("Bob");
    }

    [Fact]
    public async Task UpdateProfileAsync_StringFieldsAreTrimmedAndWhitespaceBecomesNull()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = null
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "  Trimmed  ",
            LastName = "   ",
            DisplayName = "\t\n",
            Gender = "  Male  ",
            Language = "",
            Timezone = "   UTC   ",
            Bio = "  My bio  ",
            Website = "     "
        };

        UserProfile? addedProfile = null;
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddProfileAsync(It.IsAny<UserProfile>(), default))
            .Callback<UserProfile, CancellationToken>((profile, _) => addedProfile = profile)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateProfileAsync(userId, dto);

        addedProfile!.FirstName.Should().Be("Trimmed");
        addedProfile.LastName.Should().BeNull();
        addedProfile.DisplayName.Should().Be("Trimmed");
        addedProfile.Gender.Should().Be("Male");
        addedProfile.Language.Should().BeNull();
        addedProfile.Timezone.Should().Be("UTC");
        addedProfile.Bio.Should().Be("My bio");
        addedProfile.Website.Should().BeNull();

        result.FirstName.Should().Be("Trimmed");
        result.LastName.Should().BeNull();
        result.DisplayName.Should().Be("Trimmed");
    }

    [Fact]
    public async Task UpdateProfileAsync_DisplayNameFallbackReturnsNullWhenAllNamesEmpty()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = null
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "   ",
            LastName = "   ",
            DisplayName = null
        };

        UserProfile? addedProfile = null;
        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddProfileAsync(It.IsAny<UserProfile>(), default))
            .Callback<UserProfile, CancellationToken>((profile, _) => addedProfile = profile)
            .Returns(Task.CompletedTask);

        var result = await _service.UpdateProfileAsync(userId, dto);

        addedProfile!.FirstName.Should().BeNull();
        addedProfile.LastName.Should().BeNull();
        addedProfile.DisplayName.Should().BeNull();
        result.DisplayName.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenDisplayNameNullInDto_AutoDerivesDisplayName()
    {
        var userId = Guid.NewGuid();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = "Original",
            LastName = "Name",
            DisplayName = null, // Profile DisplayName is null; DTO explicitly has null DisplayName, so fallback auto-derives from FirstName + LastName
            Gender = "Male",
            Language = "en",
            Timezone = "UTC",
            Bio = "Original bio",
            Website = "https://original.com",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-5)
        };
        var user = new User
        {
            Id = userId,
            Email = "test@example.com",
            Username = "testuser",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            Profile = profile
        };
        var dto = new UpdateProfileDto
        {
            FirstName = "Updated",
            Bio = "Updated bio"
        };

        _userRepositoryMock.Setup(x => x.GetWithProfileAsync(userId, default)).ReturnsAsync(user);

        var result = await _service.UpdateProfileAsync(userId, dto);

        profile.FirstName.Should().Be("Updated");
        profile.LastName.Should().Be("Name");
        // Profile DisplayName was null and DTO does not provide DisplayName, so fallback auto-derives
        profile.DisplayName.Should().Be("Updated Name");
        profile.Gender.Should().Be("Male");
        profile.Language.Should().Be("en");
        profile.Timezone.Should().Be("UTC");
        profile.Bio.Should().Be("Updated bio");
        profile.Website.Should().Be("https://original.com");
    }

    [Fact]
    public async Task UpdateProfileAsync_WhenDtoIsNull_ThrowsArgumentNullException()
    {
        var userId = Guid.NewGuid();

        var act = () => _service.UpdateProfileAsync(userId, null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("dto");
    }
}
