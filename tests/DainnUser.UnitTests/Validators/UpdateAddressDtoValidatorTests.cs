using DainnUser.Application.Validators;
using DainnUser.Core.Models.Address;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class UpdateAddressDtoValidatorTests
{
    private readonly UpdateAddressDtoValidator _validator;

    public UpdateAddressDtoValidatorTests()
    {
        _validator = new UpdateAddressDtoValidator();
    }

    [Fact]
    public void Validate_WithAllNullFields_ShouldNotHaveErrors()
    {
        var dto = new UpdateAddressDto();

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        var dto = new UpdateAddressDto
        {
            AddressLine1 = "456 New St",
            City = "HCMC",
            Country = "Vietnam",
            AddressLine2 = "Suite 100",
            StateProvince = "HCMC",
            PostalCode = "700000",
            AddressType = "Work"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithTooLongAddressLine1_ShouldHaveError()
    {
        var dto = new UpdateAddressDto
        {
            AddressLine1 = new string('a', 501)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithTooLongCity_ShouldHaveError()
    {
        var dto = new UpdateAddressDto
        {
            City = new string('a', 101)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorMessage("City must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithTooLongCountry_ShouldHaveError()
    {
        var dto = new UpdateAddressDto
        {
            Country = new string('a', 101)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country must not exceed 100 characters.");
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("ABC-123")]
    [InlineData("SW1A 1AA")]
    public void Validate_WithValidPostalCode_ShouldNotHaveError(string postalCode)
    {
        var dto = new UpdateAddressDto { PostalCode = postalCode };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.PostalCode);
    }

    [Theory]
    [InlineData("ABC@123")]
    [InlineData("12345!")]
    public void Validate_WithInvalidPostalCode_ShouldHaveError(string postalCode)
    {
        var dto = new UpdateAddressDto { PostalCode = postalCode };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.PostalCode)
            .WithErrorMessage("Postal code must be alphanumeric with spaces or dashes, max 20 characters.");
    }

    [Fact]
    public void Validate_WithTooLongPostalCode_ShouldHaveError()
    {
        var dto = new UpdateAddressDto { PostalCode = new string('1', 21) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.PostalCode)
            .WithErrorMessage("Postal code must not exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithTooLongAddressType_ShouldHaveError()
    {
        var dto = new UpdateAddressDto { AddressType = new string('a', 51) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressType)
            .WithErrorMessage("Address type must not exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithTooLongAddressLine2_ShouldHaveError()
    {
        var dto = new UpdateAddressDto { AddressLine2 = new string('a', 501) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressLine2)
            .WithErrorMessage("Address line 2 must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithTooLongStateProvince_ShouldHaveError()
    {
        var dto = new UpdateAddressDto { StateProvince = new string('a', 101) };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.StateProvince)
            .WithErrorMessage("State/Province must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithNullFields_ShouldNotHaveErrors()
    {
        var dto = new UpdateAddressDto
        {
            AddressLine1 = null,
            City = null,
            Country = null,
            AddressLine2 = null,
            StateProvince = null,
            PostalCode = null,
            AddressType = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
