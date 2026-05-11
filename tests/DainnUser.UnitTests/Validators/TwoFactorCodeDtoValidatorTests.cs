using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class TwoFactorCodeDtoValidatorTests
{
    private readonly TwoFactorCodeDtoValidator _validator;

    public TwoFactorCodeDtoValidatorTests()
    {
        _validator = new TwoFactorCodeDtoValidator();
    }

    [Fact]
    public void Validate_WithValidCode_ShouldNotHaveErrors()
    {
        // Arrange
        var dto = new TwoFactorCodeDto
        {
            Code = "123456"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_WithEmptyCode_ShouldHaveError(string code)
    {
        // Arrange
        var dto = new TwoFactorCodeDto
        {
            Code = code!
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
        var dto = new TwoFactorCodeDto
        {
            Code = new string('1', 33) // > 32 chars
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Code)
            .WithErrorMessage("Code must not exceed 32 characters.");
    }

    [Theory]
    [InlineData("123456")] // 6 digits (TOTP)
    [InlineData("12345678")] // 8 digits (backup code)
    [InlineData("ABCD1234")] // Alphanumeric backup code
    public void Validate_WithValidCodeFormats_ShouldNotHaveError(string code)
    {
        // Arrange
        var dto = new TwoFactorCodeDto
        {
            Code = code
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Code);
    }
}
