using System.Text.RegularExpressions;
using DainnUser.Core.Models.Contact;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for updating contacts.
/// </summary>
public partial class UpdateContactDtoValidator : AbstractValidator<UpdateContactDto>
{
    [GeneratedRegex(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_]{1,64}$", RegexOptions.Compiled)]
    private static partial Regex SocialPattern();

    public UpdateContactDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.ContactType) || !string.IsNullOrWhiteSpace(x.ContactValue))
            .WithMessage("At least one field must be provided.");

        RuleFor(x => x.ContactType)
            .MaximumLength(50).WithMessage("Contact type must not exceed 50 characters.")
            .When(x => x.ContactType is not null);

        RuleFor(x => x.ContactValue)
            .MaximumLength(256).WithMessage("Contact value must not exceed 256 characters.")
            .When(x => x.ContactValue is not null);

        When(x => AddContactDtoValidator.IsPhoneType(x.ContactType) && !string.IsNullOrWhiteSpace(x.ContactValue), () =>
        {
            RuleFor(x => x.ContactValue!)
                .Matches(PhonePattern())
                .WithMessage("Phone number must be in E.164 format, for example +84901234567.");
        });

        When(x => AddContactDtoValidator.IsEmailType(x.ContactType) && !string.IsNullOrWhiteSpace(x.ContactValue), () =>
        {
            RuleFor(x => x.ContactValue!)
                .Matches(EmailPattern())
                .WithMessage("Contact value must be a valid email address.");
        });

        When(x => AddContactDtoValidator.IsSocialType(x.ContactType) && !string.IsNullOrWhiteSpace(x.ContactValue), () =>
        {
            RuleFor(x => x.ContactValue!)
                .Matches(SocialPattern())
                .WithMessage("Social contact value may contain only letters, numbers, and underscores.");
        });
    }
}
