using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class ChangePasswordDtoValidatorTests
{
    private readonly ChangePasswordDtoValidator _validator;

    public ChangePasswordDtoValidatorTests()
    {
        _validator = new ChangePasswordDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!@#",
            NewPassword = "NewPass123!@#",
            ConfirmNewPassword = "NewPass123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyCurrentPassword_ShouldHaveError(string currentPassword)
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = currentPassword!,
            NewPassword = "NewPass123!@#",
            ConfirmNewPassword = "NewPass123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentPassword)
            .WithErrorMessage("Current password is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyNewPassword_ShouldHaveError(string newPassword)
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!@#",
            NewPassword = newPassword!,
            ConfirmNewPassword = newPassword!
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
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!@#",
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword
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
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!@#",
            NewPassword = newPassword,
            ConfirmNewPassword = newPassword
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword);
    }

    [Fact]
    public void Validate_WithSamePassword_ShouldHaveError()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "SamePass123!@#",
            NewPassword = "SamePass123!@#",
            ConfirmNewPassword = "SamePass123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.NewPassword)
            .WithErrorMessage("New password must be different from the current password.");
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldHaveError()
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!@#",
            NewPassword = "NewPass123!@#",
            ConfirmNewPassword = "Different123!@#"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword)
            .WithErrorMessage("Passwords do not match.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyConfirmPassword_ShouldHaveError(string confirmPassword)
    {
        // Arrange
        var dto = new ChangePasswordDto
        {
            CurrentPassword = "OldPass123!@#",
            NewPassword = "NewPass123!@#",
            ConfirmNewPassword = confirmPassword!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmNewPassword)
            .WithErrorMessage("Password confirmation is required.");
    }
}
