using DainnUser.Api.DTOs;
using DainnUser.Api.DTOs.Authentication;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DainnUser.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    public AuthController(
        IAuthenticationService authenticationService,
        IValidator<RegisterDto> registerValidator,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _registerValidator = registerValidator;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration response.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<RegisterResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate request
            var validationResult = await _registerValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse(
                    "Validation failed.",
                    errors));
            }

            // Register user
            var userId = await _authenticationService.RegisterAsync(
                request.Email,
                request.Username,
                request.Password,
                cancellationToken);

            var response = new RegisterResponse
            {
                UserId = userId,
                Message = "Registration successful. Please check your email to verify your account."
            };

            return CreatedAtAction(
                nameof(Register),
                ApiResponse<RegisterResponse>.SuccessResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);
            return Conflict(ApiResponse<RegisterResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during registration");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<RegisterResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Verifies a user's email address.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verification result.</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authenticationService.VerifyEmailAsync(
                request.UserId,
                request.Token,
                cancellationToken);

            if (!result)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(
                    "Email verification failed. Token may be invalid, expired, or already used."));
            }

            return Ok(ApiResponse<string>.SuccessResponse(
                "Email verified successfully.",
                "Your account is now active."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during email verification");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Resends the verification email.
    /// </summary>
    /// <param name="request">The resend request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result.</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendVerification(
        [FromBody] ResendVerificationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authenticationService.ResendVerificationEmailAsync(
                request.Email,
                cancellationToken);

            if (!result)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(
                    "Failed to resend verification email. User may not exist or email is already verified."));
            }

            return Ok(ApiResponse<string>.SuccessResponse(
                "Verification email sent.",
                "Please check your email for the verification link."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during resend verification");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }
}
