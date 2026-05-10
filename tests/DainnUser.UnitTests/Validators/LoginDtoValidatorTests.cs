using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidInput_Passes()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = "Pass123!@#" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingEmail_Fails(string email)
    {
        var dto = new LoginDto { Email = email, Password = "Pass123!@#" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void Validate_WithInvalidEmail_Fails(string email)
    {
        var dto = new LoginDto { Email = email, Password = "Pass123!@#" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithEmailTooLong_Fails()
    {
        var longLocal = new string('a', 250);
        var dto = new LoginDto { Email = $"{longLocal}@example.com", Password = "Pass123!@#" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithMissingPassword_Fails()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = "" };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithPasswordTooLong_Fails()
    {
        var dto = new LoginDto { Email = "user@example.com", Password = new string('p', 129) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_DoesNotEnforceComplexityRules_OnLogin()
    {
        // Login should NOT enforce password complexity (would block legitimate users with passwords
        // created before the policy was strengthened, and leak the policy to attackers).
        var dto = new LoginDto { Email = "user@example.com", Password = "weak" };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
