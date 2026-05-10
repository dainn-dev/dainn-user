using System.Security.Claims;
using DainnUser.Api.DTOs;
using DainnUser.Api.DTOs.Authentication;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
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
    private readonly ITwoFactorService? _twoFactorService;
    private readonly IValidator<RegisterDto> _registerValidator;
    private readonly IValidator<LoginDto> _loginValidator;
    private readonly IValidator<RefreshTokenDto> _refreshValidator;
    private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
    private readonly IValidator<ChangePasswordDto> _changePasswordValidator;
    private readonly IValidator<TwoFactorCodeDto> _twoFactorCodeValidator;
    private readonly IValidator<CompleteTwoFactorLoginDto> _completeTwoFactorLoginValidator;
    private readonly ILogger<AuthController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    public AuthController(
        IAuthenticationService authenticationService,
        ITwoFactorService? twoFactorService,
        IValidator<RegisterDto> registerValidator,
        IValidator<LoginDto> loginValidator,
        IValidator<RefreshTokenDto> refreshValidator,
        IValidator<ForgotPasswordDto> forgotPasswordValidator,
        IValidator<ResetPasswordDto> resetPasswordValidator,
        IValidator<ChangePasswordDto> changePasswordValidator,
        IValidator<TwoFactorCodeDto> twoFactorCodeValidator,
        IValidator<CompleteTwoFactorLoginDto> completeTwoFactorLoginValidator,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _twoFactorService = twoFactorService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _refreshValidator = refreshValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _changePasswordValidator = changePasswordValidator;
        _twoFactorCodeValidator = twoFactorCodeValidator;
        _completeTwoFactorLoginValidator = completeTwoFactorLoginValidator;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    /// <param name="request">The registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration response.</returns>
    [HttpPost("register")]
    [AllowAnonymous]
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
    /// Authenticates a user and returns JWT access and refresh tokens.
    /// </summary>
    /// <param name="request">The login request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The login response with tokens and user information.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Validation failed.", errors));
            }

            var result = await _authenticationService.LoginAsync(
                request.Email,
                request.Password,
                GetClientIp(),
                Request.Headers.UserAgent.ToString(),
                request.RememberDeviceToken,
                cancellationToken);

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(LoginResponse.FromResult(result)));
        }
        catch (InvalidCredentialsException)
        {
            // Generic message - do not disclose whether the email exists.
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid email or password."));
        }
        catch (EmailNotVerifiedException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
        catch (AccountLockedException ex)
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
        catch (AccountInactiveException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<LoginResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Manually unlocks a user's account (admin operation). Resets the failed-login counter and
    /// clears any active lockout. Restores <c>Locked</c> status to <c>Active</c>. Idempotent.
    /// Requires the caller to hold the <c>Administrator</c> role.
    /// </summary>
    /// <param name="userId">The user identifier to unlock.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>200 on success, 404 if the user does not exist.</returns>
    [HttpPost("admin/unlock-account/{userId:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockAccount(
        [FromRoute] Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var found = await _authenticationService.UnlockAccountAsync(userId, cancellationToken);
            if (!found)
            {
                return NotFound(ApiResponse<string>.ErrorResponse("User not found."));
            }

            _logger.LogInformation("Admin manually unlocked user {UserId}", userId);
            return Ok(ApiResponse<string>.SuccessResponse(
                "Account unlocked.",
                "Failed login counters reset and lockout cleared."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error unlocking user {UserId}", userId);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Logs out the current session: deactivates the session row and revokes the associated
    /// refresh token. Requires a valid JWT bearer token. Idempotent — returns 200 even when the
    /// session has already been ended.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        try
        {
            var sessionId = ResolveSessionId(User);
            if (sessionId == Guid.Empty)
            {
                // Token is valid (passed [Authorize]) but missing sid claim — treat as success
                // since there's nothing to revoke. This is defensive: a malformed token won't 500.
                return Ok(ApiResponse<string>.SuccessResponse("Logged out.", "No session to revoke."));
            }

            await _authenticationService.LogoutAsync(sessionId, cancellationToken);

            return Ok(ApiResponse<string>.SuccessResponse("Logged out.", "Session ended."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during logout");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Refreshes an access token using a previously issued refresh token. Rotates the refresh token —
    /// the supplied token cannot be reused.
    /// </summary>
    /// <param name="request">The refresh request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A new access token, refresh token, and session metadata.</returns>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status423Locked)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _refreshValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("Validation failed.", errors));
            }

            var result = await _authenticationService.RefreshTokenAsync(
                request.RefreshToken,
                GetClientIp(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(LoginResponse.FromResult(result)));
        }
        catch (InvalidRefreshTokenException ex)
        {
            if (ex.IsReuseDetected)
            {
                _logger.LogWarning("Refresh token reuse detected — all sessions revoked.");
            }
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid refresh token."));
        }
        catch (AccountLockedException ex)
        {
            return StatusCode(
                StatusCodes.Status423Locked,
                ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
        catch (AccountInactiveException ex)
        {
            return StatusCode(
                StatusCodes.Status403Forbidden,
                ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token refresh");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<LoginResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Verifies a user's email address.
    /// </summary>
    /// <param name="request">The verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verification result.</returns>
    [HttpPost("verify-email")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    /// <summary>
    /// Initiates a password reset by sending a token to the specified email address. Always returns
    /// 200 regardless of whether the email is registered, to prevent user enumeration.
    /// </summary>
    /// <param name="request">The forgot-password request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A generic success message.</returns>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _forgotPasswordValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<string>.ErrorResponse("Validation failed.", errors));
            }

            await _authenticationService.ForgotPasswordAsync(request.Email, cancellationToken);

            // Always return the same message — do not reveal whether the email is registered.
            return Ok(ApiResponse<string>.SuccessResponse(
                "If an account with that email exists, a password reset link has been sent."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during forgot-password");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Completes a password reset using the token delivered via email. Invalidates all active
    /// sessions and refresh tokens. Sends a confirmation notification to the account owner.
    /// </summary>
    /// <param name="request">The reset-password request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<string>.ErrorResponse("Validation failed.", errors));
            }

            await _authenticationService.ResetPasswordAsync(request.Token, request.NewPassword, cancellationToken);

            return Ok(ApiResponse<string>.SuccessResponse(
                "Password reset successfully.",
                "All active sessions have been invalidated. Please log in with your new password."));
        }
        catch (Core.Exceptions.InvalidPasswordResetTokenException)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse(
                "Invalid or expired password reset token."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during password reset");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Changes the authenticated user's password. Verifies the current password, updates the hash,
    /// and invalidates all other active sessions (forcing re-login on other devices).
    /// </summary>
    /// <param name="request">The change-password request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<string>.ErrorResponse("Validation failed.", errors));
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Invalid token."));
            }

            var sessionId = ResolveSessionId(User);

            await _authenticationService.ChangePasswordAsync(
                userId,
                sessionId,
                request.CurrentPassword,
                request.NewPassword,
                cancellationToken);

            return Ok(ApiResponse<string>.SuccessResponse(
                "Password changed successfully.",
                "All other active sessions have been invalidated."));
        }
        catch (Core.Exceptions.InvalidCurrentPasswordException)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse("Current password is incorrect."));
        }
        catch (Core.Exceptions.AccountInactiveException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<string>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during change-password");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Initiates two-factor authentication setup. Returns the TOTP secret and an otpauth URI
    /// to scan with an authenticator app. Must be confirmed with <see cref="EnableTwoFactor"/>
    /// before 2FA is active.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The TOTP secret and otpauth URI.</returns>
    [HttpPost("2fa/setup")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TwoFactorSetupResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TwoFactorSetupResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TwoFactorSetupResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken cancellationToken)
    {
        if (_twoFactorService is null)
        {
            return BadRequest(ApiResponse<TwoFactorSetupResponse>.ErrorResponse("Two-factor authentication is not enabled."));
        }

        try
        {
            var (userId, email) = ResolveUserIdAndEmail(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<TwoFactorSetupResponse>.ErrorResponse("Invalid token."));
            }

            var result = await _twoFactorService.PrepareEnableAsync(userId, email, cancellationToken);
            return Ok(ApiResponse<TwoFactorSetupResponse>.SuccessResponse(
                TwoFactorSetupResponse.FromResult(result),
                "Scan the QR code with your authenticator app and confirm with a code."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TwoFactorSetupResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during 2FA setup");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<TwoFactorSetupResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Confirms and activates two-factor authentication using the first code from the authenticator app.
    /// Returns a list of one-time backup codes.
    /// </summary>
    /// <param name="request">The confirmation code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of backup codes.</returns>
    [HttpPost("2fa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BackupCodesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BackupCodesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BackupCodesResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> EnableTwoFactor(
        [FromBody] TwoFactorCodeDto request,
        CancellationToken cancellationToken)
    {
        if (_twoFactorService is null)
        {
            return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse("Two-factor authentication is not enabled."));
        }

        try
        {
            var validationResult = await _twoFactorCodeValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse(
                    "Validation failed.",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var (userId, _) = ResolveUserIdAndEmail(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<BackupCodesResponse>.ErrorResponse("Invalid token."));
            }

            var backupCodes = await _twoFactorService.EnableTwoFactorAsync(userId, request.Code, cancellationToken);
            return Ok(ApiResponse<BackupCodesResponse>.SuccessResponse(
                new BackupCodesResponse { BackupCodes = backupCodes },
                "Two-factor authentication enabled. Store these backup codes securely — they will not be shown again."));
        }
        catch (Core.Exceptions.InvalidTwoFactorCodeException)
        {
            return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse("Invalid or expired two-factor code."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error enabling 2FA");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<BackupCodesResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Disables two-factor authentication. Requires a valid TOTP or backup code to confirm.
    /// Revokes all backup codes and remember-device tokens.
    /// </summary>
    /// <param name="request">A valid TOTP or backup code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A confirmation message.</returns>
    [HttpPost("2fa/disable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DisableTwoFactor(
        [FromBody] TwoFactorCodeDto request,
        CancellationToken cancellationToken)
    {
        if (_twoFactorService is null)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse("Two-factor authentication is not enabled."));
        }

        try
        {
            var validationResult = await _twoFactorCodeValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse<string>.ErrorResponse(
                    "Validation failed.",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var (userId, _) = ResolveUserIdAndEmail(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<string>.ErrorResponse("Invalid token."));
            }

            await _twoFactorService.DisableTwoFactorAsync(userId, request.Code, cancellationToken);
            return Ok(ApiResponse<string>.SuccessResponse("Two-factor authentication disabled."));
        }
        catch (Core.Exceptions.InvalidTwoFactorCodeException)
        {
            return BadRequest(ApiResponse<string>.ErrorResponse("Invalid or expired two-factor code."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error disabling 2FA");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<string>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Completes a login that required two-factor authentication. Verifies the TOTP or backup code
    /// and issues access/refresh tokens.
    /// </summary>
    /// <param name="request">The 2FA challenge completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full login response with tokens.</returns>
    [HttpPost("2fa/login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteTwoFactorLogin(
        [FromBody] CompleteTwoFactorLoginDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _completeTwoFactorLoginValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(
                    "Validation failed.",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var result = await _authenticationService.CompleteTwoFactorLoginAsync(
                request.UserId,
                request.Code,
                request.RememberDevice,
                GetClientIp(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(LoginResponse.FromResult(result)));
        }
        catch (Core.Exceptions.InvalidTwoFactorCodeException)
        {
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("Invalid or expired two-factor code."));
        }
        catch (Core.Exceptions.AccountInactiveException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error completing 2FA login");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<LoginResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Regenerates ten fresh backup codes. Requires a valid TOTP code (backup codes are not accepted
    /// here to prevent exhaustion attacks). Old backup codes are immediately revoked.
    /// </summary>
    /// <param name="request">A valid TOTP code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new backup codes.</returns>
    [HttpPost("2fa/backup-codes/regenerate")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<BackupCodesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BackupCodesResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BackupCodesResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegenerateBackupCodes(
        [FromBody] TwoFactorCodeDto request,
        CancellationToken cancellationToken)
    {
        if (_twoFactorService is null)
        {
            return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse("Two-factor authentication is not enabled."));
        }

        try
        {
            var validationResult = await _twoFactorCodeValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse(
                    "Validation failed.",
                    validationResult.Errors.Select(e => e.ErrorMessage).ToList()));
            }

            var (userId, _) = ResolveUserIdAndEmail(User);
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<BackupCodesResponse>.ErrorResponse("Invalid token."));
            }

            var backupCodes = await _twoFactorService.RegenerateBackupCodesAsync(userId, request.Code, cancellationToken);
            return Ok(ApiResponse<BackupCodesResponse>.SuccessResponse(
                new BackupCodesResponse { BackupCodes = backupCodes },
                "Backup codes regenerated. All previous codes are now invalid."));
        }
        catch (Core.Exceptions.InvalidTwoFactorCodeException)
        {
            return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse("Invalid or expired two-factor code."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BackupCodesResponse>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error regenerating backup codes");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<BackupCodesResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    private string? GetClientIp()
    {
        // Honor X-Forwarded-For when configured (consumers should also call UseForwardedHeaders).
        var forwarded = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private static (Guid UserId, string Email) ResolveUserIdAndEmail(ClaimsPrincipal principal)
    {
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst("email")?.Value
                    ?? string.Empty;

        return Guid.TryParse(userIdClaim, out var userId)
            ? (userId, email)
            : (Guid.Empty, email);
    }

    private static Guid ResolveSessionId(ClaimsPrincipal principal)
    {
        var sid = principal.FindFirst("sid")?.Value;
        return Guid.TryParse(sid, out var sessionId) ? sessionId : Guid.Empty;
    }
}
