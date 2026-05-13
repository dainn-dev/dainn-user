using System.Text.RegularExpressions;
using DainnUser.Core.Models.Address;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for UpdateAddressDto.
/// </summary>
public class UpdateAddressDtoValidator : AbstractValidator<UpdateAddressDto>
{
    private static readonly Regex PostalCodePattern = new(@"^[a-zA-Z0-9\s\-]{1,20}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAddressDtoValidator"/> class.
    /// </summary>
    public UpdateAddressDtoValidator()
    {
        RuleFor(x => x.AddressLine1)
            .MaximumLength(500).WithMessage("Address line 1 must not exceed 500 characters.")
            .When(x => x.AddressLine1 is not null);

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(x => x.City is not null);

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(x => x.Country is not null);

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters.")
            .Matches(PostalCodePattern).When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithMessage("Postal code must be alphanumeric with spaces or dashes, max 20 characters.");

        RuleFor(x => x.AddressType)
            .MaximumLength(50).WithMessage("Address type must not exceed 50 characters.")
            .When(x => x.AddressType is not null);

        RuleFor(x => x.AddressLine2)
            .MaximumLength(500).WithMessage("Address line 2 must not exceed 500 characters.")
            .When(x => x.AddressLine2 is not null);

        RuleFor(x => x.StateProvince)
            .MaximumLength(100).WithMessage("State/Province must not exceed 100 characters.")
            .When(x => x.StateProvince is not null);
    }
}
