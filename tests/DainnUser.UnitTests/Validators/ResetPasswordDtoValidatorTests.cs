using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class ResetPasswordDtoValidatorTests
{
    private readonly ResetPasswordDtoValidator _validator;

    public ResetPasswordDtoValidatorTests()
    {
        _validator = new ResetPasswordDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "valid-reset-token-12345",
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "NewPass123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyToken_ShouldHaveError(string token)
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = token!,
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "NewPass123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token)
            .WithErrorMessage("Reset token is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyNewPassword_ShouldHaveError(string newPassword)
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "valid-reset-token-12345",
            NewPassword = newPassword!,
            ConfirmPassword = newPassword!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password is required.");
    }

    [Theory]
    [InlineData("Test12!")] // Too short
    public void Validate_WithTooShortNewPassword_ShouldHaveError(string newPassword)
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "valid-reset-token-12345",
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("Password must be at least 8 characters.");
    }

    [Theory]
    [InlineData("test123!@#")] // No uppercase
    [InlineData("TEST123!@#")] // No lowercase
    [InlineData("TestTest!@#")] // No digit
    [InlineData("Test123456")] // No special char
    public void Validate_WithWeakNewPassword_ShouldHaveError(string newPassword)
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "valid-reset-token-12345",
            NewPassword = newPassword,
            ConfirmPassword = newPassword
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldHaveError()
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "valid-reset-token-12345",
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "Different123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyConfirmPassword_ShouldHaveError(string confirmPassword)
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = "valid-reset-token-12345",
            NewPassword = "NewPass123!@#",
            ConfirmPassword = confirmPassword!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Password confirmation is required.");
    }

    [Fact]
    public void Validate_WithTooLongToken_ShouldHaveError()
    {
        // Arrange
        var dto = new ResetPasswordDto
        {
            Token = new string('a', 2049), // > 2048 chars
            NewPassword = "NewPass123!@#",
            ConfirmPassword = "NewPass123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Token)
            .WithErrorMessage("Reset token is too long.");
    }
}
