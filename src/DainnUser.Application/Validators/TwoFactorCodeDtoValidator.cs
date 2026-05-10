using DainnUser.Application.DTOs.Authentication;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for <see cref="TwoFactorCodeDto"/>.
/// </summary>
public class TwoFactorCodeDtoValidator : AbstractValidator<TwoFactorCodeDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorCodeDtoValidator"/> class.
    /// </summary>
    public TwoFactorCodeDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(32).WithMessage("Code must not exceed 32 characters.");
    }
}
