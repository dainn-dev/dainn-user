using DainnUser.Application.DTOs.Authentication;
using DainnUser.Application.Validators;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class RefreshTokenDtoValidatorTests
{
    private readonly RefreshTokenDtoValidator _validator = new();

    [Fact]
    public void Validate_WithValidToken_Passes()
    {
        var dto = new RefreshTokenDto { RefreshToken = "abc-some-token-value" };
        _validator.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithMissingToken_Fails(string token)
    {
        _validator.TestValidate(new RefreshTokenDto { RefreshToken = token })
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void Validate_WithExcessivelyLongToken_Fails()
    {
        _validator.TestValidate(new RefreshTokenDto { RefreshToken = new string('a', 2049) })
            .ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}
