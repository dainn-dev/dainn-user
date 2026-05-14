using System.Text.RegularExpressions;
using DainnUser.Core.Models.Contact;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for adding contacts.
/// </summary>
public partial class AddContactDtoValidator : AbstractValidator<AddContactDto>
{
    [GeneratedRegex(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[A-Za-z0-9_]{1,64}$", RegexOptions.Compiled)]
    private static partial Regex SocialPattern();

    public AddContactDtoValidator()
    {
        RuleFor(x => x.ContactType)
            .NotEmpty().WithMessage("Contact type is required.")
            .MaximumLength(50).WithMessage("Contact type must not exceed 50 characters.");

        RuleFor(x => x.ContactValue)
            .NotEmpty().WithMessage("Contact value is required.")
            .MaximumLength(256).WithMessage("Contact value must not exceed 256 characters.");

        When(x => IsPhoneType(x.ContactType), () =>
        {
            RuleFor(x => x.ContactValue)
                .Matches(PhonePattern())
                .WithMessage("Phone number must be in E.164 format, for example +84901234567.");
        });

        When(x => IsEmailType(x.ContactType), () =>
        {
            RuleFor(x => x.ContactValue)
                .Matches(EmailPattern())
                .WithMessage("Contact value must be a valid email address.");
        });

        When(x => IsSocialType(x.ContactType), () =>
        {
            RuleFor(x => x.ContactValue)
                .Matches(SocialPattern())
                .WithMessage("Social contact value may contain only letters, numbers, and underscores.");
        });
    }

    internal static bool IsPhoneType(string? contactType) =>
        string.Equals(contactType, "Phone", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contactType, "Mobile", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contactType, "WhatsApp", StringComparison.OrdinalIgnoreCase);

    internal static bool IsEmailType(string? contactType) =>
        string.Equals(contactType, "Email", StringComparison.OrdinalIgnoreCase);

    internal static bool IsSocialType(string? contactType) =>
        string.Equals(contactType, "Telegram", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(contactType, "Skype", StringComparison.OrdinalIgnoreCase);
}
