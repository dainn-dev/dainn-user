using DainnUser.Application.Validators;
using DainnUser.Core.Models.Contact;
using FluentAssertions;

namespace DainnUser.UnitTests.Validators;

public class AddContactDtoValidatorTests
{
    private readonly AddContactDtoValidator _validator = new();

    [Fact]
    public void Validate_WhenRequiredFieldsMissing_Fails()
    {
        var result = _validator.Validate(new AddContactDto());

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddContactDto.ContactType));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddContactDto.ContactValue));
    }

    [Theory]
    [InlineData("Phone", "+84901234567")]
    [InlineData("WhatsApp", "+14155552671")]
    [InlineData("Email", "user@example.com")]
    [InlineData("Telegram", "valid_user_123")]
    public void Validate_WhenValidContact_Succeeds(string contactType, string contactValue)
    {
        var result = _validator.Validate(new AddContactDto { ContactType = contactType, ContactValue = contactValue });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Phone", "0901234567")]
    [InlineData("WhatsApp", "not-phone")]
    [InlineData("Email", "not-email")]
    [InlineData("Telegram", "bad user!")]
    public void Validate_WhenFormatInvalid_Fails(string contactType, string contactValue)
    {
        var result = _validator.Validate(new AddContactDto { ContactType = contactType, ContactValue = contactValue });

        result.IsValid.Should().BeFalse();
    }
}
