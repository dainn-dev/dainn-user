using DainnUser.Application.Validators;
using DainnUser.Core.Models.Profile;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class UpdateProfileDtoValidatorTests
{
    private readonly UpdateProfileDtoValidator _validator;

    public UpdateProfileDtoValidatorTests()
    {
        _validator = new UpdateProfileDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            DisplayName = "JohnD",
            Bio = "Software developer",
            DateOfBirth = new DateTime(1990, 1, 1),
            Language = "en",
            Timezone = "UTC",
            Website = "https://example.com"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithDateOfBirthInFuture_ShouldHaveError()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            DateOfBirth = DateTime.UtcNow.Date.AddDays(1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("Date of birth cannot be in the future.");
    }

    [Theory]
    [InlineData("eng")] // Three letters
    [InlineData("e")] // One letter
    [InlineData("123")] // Numbers
    public void Validate_WithInvalidLanguageCode_ShouldHaveError(string language)
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            Language = language
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Language)
            .WithErrorMessage("Language must be a valid two-letter ISO 639-1 code (e.g., 'en', 'vi').");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("vi")]
    [InlineData("fr")]
    [InlineData("EN")] // Case insensitive
    public void Validate_WithValidLanguageCode_ShouldNotHaveError(string language)
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            Language = language
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Language);
    }

    [Theory]
    [InlineData("Invalid/Timezone")]
    [InlineData("NotATimezone")]
    public void Validate_WithInvalidTimezone_ShouldHaveError(string timezone)
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            Timezone = timezone
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Timezone)
            .WithErrorMessage("Timezone must be a valid system timezone identifier (e.g., 'Asia/Ho_Chi_Minh', 'UTC').");
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("Asia/Ho_Chi_Minh")]
    [InlineData("America/New_York")]
    public void Validate_WithValidTimezone_ShouldNotHaveError(string timezone)
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            Timezone = timezone
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Timezone);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://example.com")] // Not HTTP/HTTPS
    [InlineData("example.com")] // No scheme
    public void Validate_WithInvalidUrl_ShouldHaveError(string website)
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            Website = website
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Website)
            .WithErrorMessage("Website must be a valid HTTP or HTTPS URL.");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("https://www.example.com/path")]
    public void Validate_WithValidUrl_ShouldNotHaveError(string website)
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            Website = website
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Website);
    }

    [Fact]
    public void Validate_WithTooLongFirstName_ShouldHaveError()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            FirstName = new string('a', 101) // > 100 chars
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithTooLongLastName_ShouldHaveError()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            LastName = new string('a', 101) // > 100 chars
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithTooLongDisplayName_ShouldHaveError()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            DisplayName = new string('a', 201) // > 200 chars
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DisplayName)
            .WithErrorMessage("Display name must not exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithTooLongBio_ShouldHaveError()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            Bio = new string('a', 501) // > 500 chars
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Bio)
            .WithErrorMessage("Bio must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithNullValues_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new UpdateProfileDto
        {
            FirstName = null,
            LastName = null,
            DisplayName = null,
            Bio = null,
            DateOfBirth = null,
            Language = null,
            Timezone = null,
            Website = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
