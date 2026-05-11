using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class ForgotPasswordDtoValidatorTests
{
    private readonly ForgotPasswordDtoValidator _validator;

    public ForgotPasswordDtoValidatorTests()
    {
        _validator = new ForgotPasswordDtoValidator();
    }

    [Fact]
    public void Validate_WithValidEmail_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new ForgotPasswordDto
        {
            Email = "test@example.com"
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
        var dto = new ForgotPasswordDto
        {
            Email = email!
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
        var dto = new ForgotPasswordDto
        {
            Email = email
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
        var dto = new ForgotPasswordDto
        {
            Email = new string('a', 250) + "@example.com" // > 256 chars
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 256 characters.");
    }
}
