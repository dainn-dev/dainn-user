using System.Text.RegularExpressions;
using DainnUser.Core.Models.Address;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for AddAddressDto.
/// </summary>
public class AddAddressDtoValidator : AbstractValidator<AddAddressDto>
{
    private static readonly Regex PostalCodePattern = new(@"^[a-zA-Z0-9\s\-]{1,20}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAddressDtoValidator"/> class.
    /// </summary>
    public AddAddressDtoValidator()
    {
        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(500).WithMessage("Address line 1 must not exceed 500 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters.")
            .Matches(PostalCodePattern).When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithMessage("Postal code must be alphanumeric with spaces or dashes, max 20 characters.");

        RuleFor(x => x.AddressType)
            .MaximumLength(50).WithMessage("Address type must not exceed 50 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(500).WithMessage("Address line 2 must not exceed 500 characters.");

        RuleFor(x => x.StateProvince)
            .MaximumLength(100).WithMessage("State/Province must not exceed 100 characters.");
    }
}
