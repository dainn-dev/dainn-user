using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class CompleteTwoFactorLoginDtoValidatorTests
{
    private readonly CompleteTwoFactorLoginDtoValidator _validator;

    public CompleteTwoFactorLoginDtoValidatorTests()
    {
        _validator = new CompleteTwoFactorLoginDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new CompleteTwoFactorLoginDto
        {
            UserId = Guid.NewGuid(),
            Code = "123456",
            RememberDevice = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyUserId_ShouldHaveError()
    {
        // Arrange
        var dto = new CompleteTwoFactorLoginDto
        {
            UserId = Guid.Empty,
            Code = "123456",
            RememberDevice = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("UserId is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyCode_ShouldHaveError(string code)
    {
        // Arrange
        var dto = new CompleteTwoFactorLoginDto
        {
            UserId = Guid.NewGuid(),
            Code = code!,
            RememberDevice = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code is required.");
    }

    [Fact]
    public void Validate_WithTooLongCode_ShouldHaveError()
    {
        // Arrange
        var dto = new CompleteTwoFactorLoginDto
        {
            UserId = Guid.NewGuid(),
            Code = new string('1', 33), // > 32 chars
            RememberDevice = false
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code must not exceed 32 characters.");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Validate_WithRememberDevice_ShouldNotHaveError(bool rememberDevice)
    {
        // Arrange
        var dto = new CompleteTwoFactorLoginDto
        {
            UserId = Guid.NewGuid(),
            Code = "123456",
            RememberDevice = rememberDevice
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
