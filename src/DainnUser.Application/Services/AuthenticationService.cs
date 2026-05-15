using System.Security.Cryptography;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using DainnUser.Core.Configuration;
using Microsoft.AspNetCore.Identity;
namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for user authentication operations.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ITwoFactorService? _twoFactorService;
    private readonly ISessionService? _sessionService;
    private readonly DainnUserOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="passwordHasher">The password hasher.</param>
    /// <param name="jwtTokenService">The JWT token service.</param>
    /// <param name="options">DainnUser configuration options.</param>
    public AuthenticationService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenService jwtTokenService,
        DainnUserOptions options,
        ITwoFactorService? twoFactorService = null,
        ISessionService? sessionService = null)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _options = options;
        _twoFactorService = twoFactorService;
        _sessionService = sessionService;
    }

    /// <inheritdoc/>
    public async Task<Guid> RegisterAsync(string email, string username, string password, CancellationToken cancellationToken = default)
    {
        // Check if email is already taken
        if (await _userRepository.IsEmailTakenAsync(email, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("Email is already registered.");
        }

        // Check if username is already taken
        if (await _userRepository.IsUsernameTakenAsync(username, cancellationToken: cancellationToken))
        {
            throw new InvalidOperationException("Username is already taken.");
        }

        // Create new user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant(),
            Username = username,
            EmailVerified = false,
            Status = UserStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        // Generate email verification token — store only the SHA-256 hash, send the plain token to the user.
        var verificationToken = GenerateSecureToken();
        var verificationTokenHash = ComputeSha256(verificationToken);

        // Save user to database first, then add token via repository (not navigation property)
        // to avoid inconsistent EF Core tracking behaviour with InMemory provider.
        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.EmailVerification,
            TokenValue = verificationTokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send verification email
        await _emailService.SendEmailVerificationAsync(email, username, verificationToken, cancellationToken);

        return user.Id;
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginAsync(
        string email,
        string password,
        string? ipAddress,
        string? userAgent,
        string? rememberDeviceToken = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidCredentialsException();
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailWithRolesAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            // Avoid user enumeration: return same generic error as wrong password.
            throw new InvalidCredentialsException();
        }

        // Account lockout check (if enabled): a still-active LockoutEnd blocks login regardless of password.
        if (_options.EnableAccountLockout && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            await RecordLoginAttemptAsync(user, false, "Account is locked.", ipAddress, userAgent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AccountLockedException(user.LockoutEnd);
        }

        // Verify password
        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (verification == PasswordVerificationResult.Failed)
        {
            await HandleFailedLoginAsync(user, ipAddress, userAgent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidCredentialsException();
        }

        // Email verification gate (if required): we still log the attempt as failed and block.
        if (_options.RequireEmailVerification && !user.EmailVerified)
        {
            await RecordLoginAttemptAsync(user, false, "Email not verified.", ipAddress, userAgent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new EmailNotVerifiedException();
        }

        // Reject inactive accounts (suspended/deactivated/locked-status).
        // Pending is allowed through here only if RequireEmailVerification is off (otherwise blocked above).
        if (user.Status is UserStatus.Suspended or UserStatus.Deactivated or UserStatus.Locked)
        {
            await RecordLoginAttemptAsync(user, false, $"Account status: {user.Status}.", ipAddress, userAgent, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AccountInactiveException(user.Status);
        }

        // Successful login - issue tokens and persist session/refresh state.
        var roleNames = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        var loginPermissions = ExtractPermissions(user.UserRoles);

        if (_options.EnableTwoFactor && user.TwoFactorEnabled)
        {
            var trusted = _twoFactorService is not null &&
                          await _twoFactorService.IsDeviceTrustedAsync(user.Id, rememberDeviceToken ?? string.Empty, cancellationToken);

            if (!trusted)
            {
                await RecordLoginAttemptAsync(user, true, "Two-factor authentication required.", ipAddress, userAgent, cancellationToken);
                user.FailedLoginAttempts = 0;
                user.LockoutEnd = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                return new LoginResult
                {
                    RequiresTwoFactor = true,
                    TwoFactorUserId = user.Id,
                    User = new AuthenticatedUserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Username = user.Username,
                        EmailVerified = user.EmailVerified,
                        Roles = roleNames
                    }
                };
            }
        }

        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

        // Store hashed refresh token (never the plain value).
        // Use repository.AddTokenAsync rather than mutating User.Tokens navigation,
        // which the EF Core InMemory provider handles inconsistently for tracked users.
        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.RefreshToken,
            TokenValue = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        // Create session before generating the access token so the JWT's sid claim
        // matches the actual session record stored in the database.
        var sessionId = Guid.NewGuid();
        if (_options.EnableSessionManagement)
        {
            if (_sessionService is not null)
            {
                var session = await _sessionService.CreateSessionAsync(
                    user.Id, refreshTokenHash, ipAddress, userAgent, cancellationToken);
                sessionId = session.Id;
            }
            else
            {
                await _unitOfWork.Sessions.AddAsync(new UserSession
                {
                    Id = sessionId,
                    UserId = user.Id,
                    SessionToken = refreshTokenHash,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshExpiresAt,
                    LastActivityAt = DateTime.UtcNow,
                    IsActive = true
                }, cancellationToken);
            }
        }

        var accessToken = GenerateAccessToken(user, roleNames, loginPermissions, sessionId);

        // Reset lockout counters on successful auth and stamp last-login.
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await RecordLoginAttemptAsync(user, true, null, ipAddress, userAgent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            SessionId = sessionId,
            User = new AuthenticatedUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                EmailVerified = user.EmailVerified,
                Roles = roleNames
            }
        };
    }

    /// <inheritdoc/>
    public async Task<LoginResult> CompleteTwoFactorLoginAsync(
        Guid userId,
        string code,
        bool rememberDevice,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (_twoFactorService is null || !_options.EnableTwoFactor)
        {
            throw new InvalidOperationException("Two-factor authentication is not enabled.");
        }

        var user = await _userRepository.GetWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new InvalidTwoFactorCodeException();
        }

        if (user.Status is UserStatus.Suspended or UserStatus.Deactivated or UserStatus.Locked)
        {
            throw new AccountInactiveException(user.Status);
        }

        var rememberDeviceToken = await _twoFactorService.VerifyTwoFactorCodeAsync(
            userId, code, rememberDevice, cancellationToken);

        var roleNames = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        var loginPermissions = ExtractPermissions(user.UserRoles);

        var sessionId = Guid.NewGuid();
        var accessToken = GenerateAccessToken(user, roleNames, loginPermissions, sessionId);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshToken);
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.RefreshToken,
            TokenValue = refreshTokenHash,
            ExpiresAt = refreshExpiresAt,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        if (_options.EnableSessionManagement)
        {
            if (_sessionService is not null)
            {
                var session = await _sessionService.CreateSessionAsync(
                    user.Id, refreshTokenHash, ipAddress, userAgent, cancellationToken);
                sessionId = session.Id;
            }
            else
            {
                await _unitOfWork.Sessions.AddAsync(new UserSession
                {
                    Id = sessionId,
                    UserId = user.Id,
                    SessionToken = refreshTokenHash,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = refreshExpiresAt,
                    LastActivityAt = DateTime.UtcNow,
                    IsActive = true
                }, cancellationToken);
            }
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        await RecordLoginAttemptAsync(user, true, null, ipAddress, userAgent, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            SessionId = sessionId,
            RequiresTwoFactor = false,
            TwoFactorRememberDeviceToken = rememberDeviceToken,
            User = new AuthenticatedUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                EmailVerified = user.EmailVerified,
                Roles = roleNames
            }
        };
    }

    /// <inheritdoc/>
    public async Task<LoginResult> RefreshTokenAsync(
        string refreshToken,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new InvalidRefreshTokenException();
        }

        var hash = _jwtTokenService.HashRefreshToken(refreshToken);
        var token = await _userRepository.GetRefreshTokenByHashAsync(hash, cancellationToken);
        if (token is null)
        {
            throw new InvalidRefreshTokenException();
        }

        // Reuse: presenting a token that has already been consumed is a strong theft signal.
        // Revoke all the user's refresh tokens and deactivate sessions, then fail.
        if (token.IsUsed)
        {
            await _userRepository.RevokeAllRefreshTokensAsync(token.UserId, cancellationToken);
            if (_options.EnableSessionManagement)
            {
                if (_sessionService is not null)
                {
                    await _sessionService.RevokeAllSessionsAsync(token.UserId, cancellationToken);
                }
                else
                {
                    await _unitOfWork.Sessions.DeactivateAllByUserIdAsync(token.UserId, cancellationToken);
                }
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidRefreshTokenException("Refresh token reuse detected.", isReuseDetected: true);
        }

        if (token.IsRevoked || token.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidRefreshTokenException();
        }

        var user = await _userRepository.GetWithRolesAsync(token.UserId, cancellationToken);
        if (user is null)
        {
            throw new InvalidRefreshTokenException();
        }

        if (user.Status is UserStatus.Suspended or UserStatus.Deactivated or UserStatus.Locked)
        {
            throw new AccountInactiveException(user.Status);
        }

        if (_options.EnableAccountLockout && user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
        {
            throw new AccountLockedException(user.LockoutEnd);
        }

        // Rotate session if we have one tied to the old hash; otherwise mint a new one.
        UserSession? session = null;
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshToken);
        var newRefreshExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);

        if (_options.EnableSessionManagement)
        {
            if (_sessionService is not null)
            {
                session = await _sessionService.RotateSessionAsync(
                    hash, newRefreshTokenHash, ipAddress, userAgent, cancellationToken);

                if (session is null)
                {
                    session = await _sessionService.CreateSessionAsync(
                        user.Id, newRefreshTokenHash, ipAddress, userAgent, cancellationToken);
                }
            }
            else
            {
                session = await _unitOfWork.Sessions.GetByTokenAsync(hash, cancellationToken);
            }
        }
        var sessionId = session?.Id ?? Guid.NewGuid();

        // Mark the presented token as used (one-time-use).
        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;

        var roleNames = user.UserRoles
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .ToList();

        var refreshPermissions = ExtractPermissions(user.UserRoles);

        var newAccessToken = GenerateAccessToken(user, roleNames, refreshPermissions, sessionId);

        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.RefreshToken,
            TokenValue = newRefreshTokenHash,
            ExpiresAt = newRefreshExpiresAt,
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        if (_options.EnableSessionManagement)
        {
            if (_sessionService is not null)
            {
                if (session is null)
                {
                    session = await _sessionService.CreateSessionAsync(
                        user.Id, newRefreshTokenHash, ipAddress, userAgent, cancellationToken);
                    sessionId = session.Id;
                }
            }
            else
            {
                if (session is not null && session.IsActive)
                {
                    // Rotate token + extend lifetime + refresh metadata for the existing session.
                    session.SessionToken = newRefreshTokenHash;
                    session.LastActivityAt = DateTime.UtcNow;
                    session.ExpiresAt = newRefreshExpiresAt;
                    if (!string.IsNullOrWhiteSpace(ipAddress)) session.IpAddress = ipAddress;
                    if (!string.IsNullOrWhiteSpace(userAgent)) session.UserAgent = userAgent;
                }
                else
                {
                    // Defensive: missing/inactive session — create a new one tied to the new hash.
                    await _unitOfWork.Sessions.AddAsync(new UserSession
                    {
                        Id = sessionId,
                        UserId = user.Id,
                        SessionToken = newRefreshTokenHash,
                        IpAddress = ipAddress,
                        UserAgent = userAgent,
                        CreatedAt = DateTime.UtcNow,
                        ExpiresAt = newRefreshExpiresAt,
                        LastActivityAt = DateTime.UtcNow,
                        IsActive = true
                    }, cancellationToken);
                }
            }
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult
        {
            AccessToken = newAccessToken.Token,
            AccessTokenExpiresAt = newAccessToken.ExpiresAt,
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAt = newRefreshExpiresAt,
            SessionId = sessionId,
            User = new AuthenticatedUserInfo
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                EmailVerified = user.EmailVerified,
                Roles = roleNames
            }
        };
    }

    /// <inheritdoc/>
    public async Task<bool> UnlockAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        if (user.Status == UserStatus.Locked)
        {
            user.Status = UserStatus.Active;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        if (sessionId == Guid.Empty)
        {
            return;
        }

        var session = await _unitOfWork.Sessions.GetByIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return; // idempotent: session already gone
        }

        // Revoke the refresh token tied to this session (same SHA-256 hash as session token).
        if (!string.IsNullOrWhiteSpace(session.SessionToken))
        {
            var token = await _userRepository.GetRefreshTokenByHashAsync(session.SessionToken, cancellationToken);
            if (token is not null && !token.IsUsed && !token.IsRevoked)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        if (session.IsActive)
        {
            session.IsActive = false;
            session.LastActivityAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> VerifyEmailAsync(Guid userId, string token, CancellationToken cancellationToken = default)
    {
        // Get user with tokens
        var user = await _userRepository.GetByIdWithTokensAsync(userId, cancellationToken);
        if (user == null)
        {
            return false;
        }

        // Find valid verification token — compare against the stored SHA-256 hash of the presented token.
        var tokenHash = ComputeSha256(token.Trim());
        var verificationToken = user.Tokens.FirstOrDefault(t =>
            t.TokenType == TokenType.EmailVerification &&
            t.TokenValue == tokenHash &&
            !t.IsUsed &&
            !t.IsRevoked &&
            t.ExpiresAt > DateTime.UtcNow);

        if (verificationToken == null)
        {
            return false;
        }

        // Mark token as used
        verificationToken.IsUsed = true;
        verificationToken.UsedAt = DateTime.UtcNow;

        // Update user
        user.EmailVerified = true;
        user.Status = UserStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;

        _userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> ResendVerificationEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // Get user by email with tokens
        var user = await _userRepository.GetByEmailWithTokensAsync(email, cancellationToken);
        if (user == null)
        {
            return false;
        }

        // Check if email is already verified
        if (user.EmailVerified)
        {
            return false;
        }

        // Revoke existing verification tokens
        var tokensToRevoke = user.Tokens
            .Where(t => t.TokenType == TokenType.EmailVerification && !t.IsUsed && !t.IsRevoked)
            .ToList();

        foreach (var existingToken in tokensToRevoke)
        {
            existingToken.IsRevoked = true;
            existingToken.RevokedAt = DateTime.UtcNow;
        }

        // Generate new verification token — store only the SHA-256 hash.
        var verificationToken = GenerateSecureToken();
        var verificationTokenHash = ComputeSha256(verificationToken);

        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.EmailVerification,
            TokenValue = verificationTokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send verification email
        await _emailService.SendEmailVerificationAsync(email, user.Username, verificationToken, cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        // Normalize and look up — never reveal whether the email exists.
        var normalized = email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailWithTokensAsync(normalized, cancellationToken);

        if (user is null || user.Status is UserStatus.Deactivated)
        {
            // No-op: caller cannot distinguish missing account from success.
            return;
        }

        // Revoke any previously issued, still-valid reset tokens to enforce single-use per request.
        foreach (var existing in user.Tokens
                     .Where(t => t.TokenType == TokenType.PasswordReset && !t.IsUsed && !t.IsRevoked))
        {
            existing.IsRevoked = true;
            existing.RevokedAt = DateTime.UtcNow;
        }

        // Generate a 32-byte URL-safe token; store its SHA-256 hash only.
        var plainToken = GenerateSecureToken();
        var tokenHash = ComputeSha256(plainToken);

        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.PasswordReset,
            TokenValue = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddHours(_options.PasswordResetTokenExpirationHours),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Best-effort email — SMTP failure must never surface as an error to the caller.
        try
        {
            await _emailService.SendPasswordResetAsync(user.Email, user.Username, plainToken, cancellationToken);
        }
        catch
        {
            // swallow — email-service already logs the failure
        }
    }

    /// <inheritdoc/>
    public async Task ResetPasswordAsync(string token, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidPasswordResetTokenException();
        }

        var hash = ComputeSha256(token.Trim());
        var userToken = await _userRepository.GetPasswordResetTokenByHashAsync(hash, cancellationToken);

        if (userToken is null || userToken.IsUsed || userToken.IsRevoked || userToken.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidPasswordResetTokenException();
        }

        var user = userToken.User;

        // Mark token consumed.
        userToken.IsUsed = true;
        userToken.UsedAt = DateTime.UtcNow;

        // Update password.
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Revoke all active refresh tokens — all existing sessions are now invalid.
        await _userRepository.RevokeAllRefreshTokensAsync(user.Id, cancellationToken);
        if (_options.EnableSessionManagement)
        {
            if (_sessionService is not null)
            {
                await _sessionService.RevokeAllSessionsAsync(user.Id, cancellationToken);
            }
            else
            {
                await _unitOfWork.Sessions.DeactivateAllByUserIdAsync(user.Id, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Best-effort confirmation notification.
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.Username, cancellationToken);
        }
        catch
        {
            // swallow — email-service already logs
        }
    }

    /// <inheritdoc/>
    public async Task ChangePasswordAsync(
        Guid userId,
        Guid currentSessionId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.Status is UserStatus.Deactivated or UserStatus.Suspended)
        {
            throw new AccountInactiveException(user?.Status ?? UserStatus.Deactivated);
        }

        // Verify current password.
        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verification == PasswordVerificationResult.Failed)
        {
            throw new InvalidCurrentPasswordException();
        }

        // Update password hash.
        user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
        user.UpdatedAt = DateTime.UtcNow;

        // Invalidate all sessions and refresh tokens EXCEPT the current session,
        // so the user stays logged in on this device.
        await _userRepository.RevokeAllRefreshTokensExceptSessionAsync(user.Id, currentSessionId, cancellationToken);
        if (_options.EnableSessionManagement)
        {
            if (_sessionService is not null)
            {
                await _sessionService.RevokeAllExceptAsync(user.Id, currentSessionId, cancellationToken);
            }
            else
            {
                await _unitOfWork.Sessions.DeactivateAllExceptAsync(user.Id, currentSessionId, cancellationToken);
            }
        }

        // Log activity.
        await _unitOfWork.ActivityLogs.AddAsync(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ActivityType = ActivityType.PasswordChange,
            Description = "Password changed by user.",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Best-effort confirmation email.
        try
        {
            await _emailService.SendPasswordChangedNotificationAsync(user.Email, user.Username, cancellationToken);
        }
        catch
        {
            // swallow — email-service already logs
        }
    }

    private async Task HandleFailedLoginAsync(
        User user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        user.FailedLoginAttempts++;
        user.UpdatedAt = DateTime.UtcNow;

        bool justLocked = false;
        if (_options.EnableAccountLockout && user.FailedLoginAttempts >= _options.MaxFailedLoginAttempts)
        {
            // Caller (LoginAsync) blocks further attempts while LockoutEnd > now, so reaching
            // this branch always represents the start of a fresh lockout cycle.
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(_options.LockoutDurationMinutes);
            justLocked = true;
        }

        await RecordLoginAttemptAsync(user, false, "Invalid credentials.", ipAddress, userAgent, cancellationToken);

        if (justLocked)
        {
            // Best-effort: notify the legitimate account owner. Email failures must never block
            // the auth pipeline (login is already failing here; an SMTP outage shouldn't change behavior).
            try
            {
                await _emailService.SendAccountLockoutNotificationAsync(
                    user.Email, user.Username, user.LockoutEnd!.Value, cancellationToken);
            }
            catch
            {
                // swallow — observability via existing email-service logging is sufficient
            }
        }
    }

    private async Task RecordLoginAttemptAsync(
        User user,
        bool isSuccessful,
        string? failureReason,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.LoginHistories.AddAsync(new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = LoginProvider.Local,
            IsSuccessful = isSuccessful,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            FailureReason = failureReason,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string ComputeSha256(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private AccessTokenResult GenerateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        Guid sessionId)
    {
        return permissions.Count == 0
            ? _jwtTokenService.GenerateAccessToken(user, roles, sessionId)
            : _jwtTokenService.GenerateAccessToken(user, roles, permissions, sessionId);
    }

    private static IReadOnlyList<string> ExtractPermissions(ICollection<UserRole> userRoles)
    {
        return userRoles
            .Select(ur => ur.Role?.Permissions)
            .Where(permissions => !string.IsNullOrWhiteSpace(permissions))
            .SelectMany(permissions => permissions!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
