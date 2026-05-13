using DainnUser.Application.Validators;
using DainnUser.Core.Models.Address;
using FluentValidation.TestHelper;

namespace DainnUser.UnitTests.Validators;

public class AddAddressDtoValidatorTests
{
    private readonly AddAddressDtoValidator _validator;

    public AddAddressDtoValidatorTests()
    {
        _validator = new AddAddressDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveErrors()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressLine2 = "Apt 4B",
            StateProvince = "Hanoi",
            PostalCode = "100000",
            AddressType = "Home"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyAddressLine1_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "",
            City = "Hanoi",
            Country = "Vietnam"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 is required.");
    }

    [Fact]
    public void Validate_WithNullAddressLine1_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = null!,
            City = "Hanoi",
            Country = "Vietnam"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 is required.");
    }

    [Fact]
    public void Validate_WithTooLongAddressLine1_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = new string('a', 501),
            City = "Hanoi",
            Country = "Vietnam"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressLine1)
            .WithErrorMessage("Address line 1 must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithEmptyCity_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "",
            Country = "Vietnam"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorMessage("City is required.");
    }

    [Fact]
    public void Validate_WithTooLongCity_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = new string('a', 101),
            Country = "Vietnam"
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.City)
            .WithErrorMessage("City must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithEmptyCountry_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = ""
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.Country)
            .WithErrorMessage("Country is required.");
    }

    [Fact]
    public void Validate_WithTooLongCountry_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
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
    [InlineData("100000")]
    public void Validate_WithValidPostalCode_ShouldNotHaveError(string postalCode)
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            PostalCode = postalCode
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveValidationErrorFor(x => x.PostalCode);
    }

    [Theory]
    [InlineData("ABC@123")]
    [InlineData("12345!")]
    [InlineData("postal_code")]
    public void Validate_WithInvalidPostalCode_ShouldHaveError(string postalCode)
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            PostalCode = postalCode
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.PostalCode)
            .WithErrorMessage("Postal code must be alphanumeric with spaces or dashes, max 20 characters.");
    }

    [Fact]
    public void Validate_WithTooLongPostalCode_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            PostalCode = new string('1', 21)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.PostalCode)
            .WithErrorMessage("Postal code must not exceed 20 characters.");
    }

    [Fact]
    public void Validate_WithTooLongAddressType_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressType = new string('a', 51)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressType)
            .WithErrorMessage("Address type must not exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithTooLongAddressLine2_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressLine2 = new string('a', 501)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.AddressLine2)
            .WithErrorMessage("Address line 2 must not exceed 500 characters.");
    }

    [Fact]
    public void Validate_WithTooLongStateProvince_ShouldHaveError()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            StateProvince = new string('a', 101)
        };

        var result = _validator.TestValidate(dto);

        result.ShouldHaveValidationErrorFor(x => x.StateProvince)
            .WithErrorMessage("State/Province must not exceed 100 characters.");
    }

    [Fact]
    public void Validate_WithNullOptionalFields_ShouldNotHaveErrors()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressLine2 = null,
            StateProvince = null,
            PostalCode = null,
            AddressType = null
        };

        var result = _validator.TestValidate(dto);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
