using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator;

    public RegisterDtoValidatorTests()
    {
        _validator = new RegisterDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyEmail_ShouldHaveError(string email)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = email!,
            Username = "testuser",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required.");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("test@")]
    [InlineData("test")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveError(string email)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = email,
            Username = "testuser",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format.");
    }

    [Fact]
    public void Validate_WithTooLongEmail_ShouldHaveError()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = new string('a', 250) + "@example.com", // > 256 chars
            Username = "testuser",
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 256 characters.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyUsername_ShouldHaveError(string username)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = username!,
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username is required.");
    }

    [Theory]
    [InlineData("ab")] // Too short
    public void Validate_WithTooShortUsername_ShouldHaveError(string username)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = username,
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must be at least 3 characters.");
    }

    [Fact]
    public void Validate_WithTooLongUsername_ShouldHaveError()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = new string('a', 51), // > 50 chars
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username must not exceed 50 characters.");
    }

    [Theory]
    [InlineData("user name")] // Space
    [InlineData("user@name")] // Special char
    [InlineData("user#name")] // Special char
    public void Validate_WithInvalidUsernameCharacters_ShouldHaveError(string username)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = username,
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("Username can only contain letters, numbers, underscores, and hyphens.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyPassword_ShouldHaveError(string password)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = password!,
            ConfirmPassword = password!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required.");
    }

    [Theory]
    [InlineData("Test12!")] // Too short
    public void Validate_WithTooShortPassword_ShouldHaveError(string password)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = password,
            ConfirmPassword = password
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Theory]
    [InlineData("test123!@#")] // No uppercase
    [InlineData("TEST123!@#")] // No lowercase
    [InlineData("TestTest!@#")] // No digit
    [InlineData("Test123456")] // No special char
    public void Validate_WithWeakPassword_ShouldHaveError(string password)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = password,
            ConfirmPassword = password
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldHaveError()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = "testuser",
            Password = "Test123!@#",
            ConfirmPassword = "Different123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match.");
    }

    [Theory]
    [InlineData("test_user")]
    [InlineData("test-user")]
    [InlineData("test123")]
    [InlineData("TestUser")]
    public void Validate_WithValidUsername_ShouldNotHaveError(string username)
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Username = username,
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }
}
