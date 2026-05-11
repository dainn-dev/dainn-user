using System.Text.RegularExpressions;
using DainnUser.Core.Models.Profile;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for UpdateProfileDto.
/// </summary>
public class UpdateProfileDtoValidator : AbstractValidator<UpdateProfileDto>
{
    private static readonly Regex TwoLetterIso639 = new(@"^[a-z]{2}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static bool IsAbsoluteHttpOrHttpsUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProfileDtoValidator"/> class.
    /// </summary>
    public UpdateProfileDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage("First name must not exceed 100 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage("Last name must not exceed 100 characters.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(100).WithMessage("Display name must not exceed 100 characters.");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Bio must not exceed 500 characters.");

        RuleFor(x => x.DateOfBirth)
            .Must(d => d is null || d <= DateTime.UtcNow.Date)
            .WithMessage("Date of birth cannot be in the future.");

        RuleFor(x => x.Language)
            .Must(l => l is null || TwoLetterIso639.IsMatch(l))
            .WithMessage("Language must be a valid two-letter ISO 639-1 code (e.g., 'en', 'vi').");

        RuleFor(x => x.Timezone)
            .Must(t => t is null || TimeZoneInfo.TryFindSystemTimeZoneById(t, out _))
            .WithMessage("Timezone must be a valid system timezone identifier (e.g., 'Asia/Ho_Chi_Minh', 'UTC').");

        RuleFor(x => x.Website)
            .Must(w => w is null || IsAbsoluteHttpOrHttpsUrl(w))
            .WithMessage("Website must be a valid HTTP or HTTPS URL.");
    }
}