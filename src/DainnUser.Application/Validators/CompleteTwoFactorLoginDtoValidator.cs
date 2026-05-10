using DainnUser.Application.DTOs.Authentication;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for <see cref="CompleteTwoFactorLoginDto"/>.
/// </summary>
public class CompleteTwoFactorLoginDtoValidator : AbstractValidator<CompleteTwoFactorLoginDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompleteTwoFactorLoginDtoValidator"/> class.
    /// </summary>
    public CompleteTwoFactorLoginDtoValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Code is required.")
            .MaximumLength(32).WithMessage("Code must not exceed 32 characters.");
    }
}
