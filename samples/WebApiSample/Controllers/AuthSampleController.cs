using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthSampleController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IProfileService _profileService;

    public AuthSampleController(
        IAuthenticationService authService,
        IProfileService profileService)
    {
        _authService = authService;
        _profileService = profileService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest dto)
    {
        var userId = await _authService.RegisterAsync(
            dto.Email,
            dto.Username,
            dto.Password);

        return Ok(new
        {
            message = "Registration successful",
            userId
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
    {
        var result = await _authService.LoginAsync(
            dto.Email,
            dto.Password,
            GetIpAddress(),
            Request.Headers.UserAgent.ToString());

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            accessTokenExpiresAt = result.AccessTokenExpiresAt,
            refreshTokenExpiresAt = result.RefreshTokenExpiresAt,
            sessionId = result.SessionId,
            user = result.User
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest dto)
    {
        var result = await _authService.RefreshTokenAsync(
            dto.RefreshToken,
            GetIpAddress(),
            Request.Headers.UserAgent.ToString());

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            accessTokenExpiresAt = result.AccessTokenExpiresAt,
            refreshTokenExpiresAt = result.RefreshTokenExpiresAt
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var sessionId = Guid.Parse(User.FindFirst("sid")?.Value ?? throw new UnauthorizedAccessException());
        await _authService.LogoutAsync(sessionId);
        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        var profile = await _profileService.GetProfileAsync(userId);

        return Ok(new
        {
            userId = profile.UserId,
            username = profile.Username,
            email = profile.Email,
            firstName = profile.FirstName,
            lastName = profile.LastName,
            displayName = profile.DisplayName,
            avatarUrl = profile.AvatarUrl,
            createdAt = profile.CreatedAt
        });
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest dto)
    {
        await _authService.ForgotPasswordAsync(dto.Email);
        return Ok(new { message = "Password reset email sent if account exists" });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest dto)
    {
        await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword);
        return Ok(new { message = "Password reset successful" });
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest dto)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? throw new UnauthorizedAccessException());
        var sessionIdStr = User.FindFirst("sid")?.Value;
        var sessionId = string.IsNullOrEmpty(sessionIdStr) ? Guid.Empty : Guid.Parse(sessionIdStr);
        await _authService.ChangePasswordAsync(userId, sessionId, dto.CurrentPassword, dto.NewPassword);
        return Ok(new { message = "Password changed successfully" });
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? RememberDeviceToken { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = "";
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}
