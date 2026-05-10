using DainnUser.Application.DTOs.Authentication;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for <see cref="RefreshTokenDto"/>.
/// </summary>
public class RefreshTokenDtoValidator : AbstractValidator<RefreshTokenDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshTokenDtoValidator"/> class.
    /// </summary>
    public RefreshTokenDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.")
            .MaximumLength(2048).WithMessage("Refresh token is too long.");
    }
}
