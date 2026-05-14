using DainnUser.Application.Validators;
using DainnUser.Core.Models.Contact;
using FluentAssertions;

namespace DainnUser.UnitTests.Validators;

public class UpdateContactDtoValidatorTests
{
    private readonly UpdateContactDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenNoFieldsProvided_Fails()
    {
        var result = _validator.Validate(new UpdateContactDto());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "At least one field must be provided.");
    }

    [Theory]
    [InlineData("Email", "user@example.com")]
    [InlineData("Phone", "+84901234567")]
    [InlineData("Telegram", "valid_user")]
    public void Validate_WhenValidUpdate_Succeeds(string contactType, string contactValue)
    {
        var result = _validator.Validate(new UpdateContactDto { ContactType = contactType, ContactValue = contactValue });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Email", "not-email")]
    [InlineData("Phone", "0901234567")]
    [InlineData("Telegram", "bad user!")]
    public void Validate_WhenFormatInvalid_Fails(string contactType, string contactValue)
    {
        var result = _validator.Validate(new UpdateContactDto { ContactType = contactType, ContactValue = contactValue });

        result.IsValid.Should().BeFalse();
    }
}
