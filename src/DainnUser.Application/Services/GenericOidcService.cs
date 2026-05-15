using System.Security.Claims;
using DainnUser.Core.Configuration;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for generic OpenID Connect (OIDC) provider authentication.
/// </summary>
public class GenericOidcService : IGenericOidcService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService? _sessionService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly DainnUserOptions _options;

    public GenericOidcService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ISessionService? sessionService,
        IPasswordHasher<User> passwordHasher,
        IOptions<DainnUserOptions> options)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _passwordHasher = passwordHasher;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginWithOidcAsync(
        string providerId,
        ClaimsPrincipal claimsPrincipal,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));

        EnsureGenericOidcEnabled();

        var providerConfig = GetProviderConfig(providerId);
        var (email, name, subject) = ExtractClaims(claimsPrincipal, providerConfig);

        if (string.IsNullOrEmpty(email))
        {
            throw new InvalidOperationException("Email claim not found in OIDC response");
        }

        if (!IsValidEmail(email))
        {
            throw new InvalidOperationException($"Invalid email format: {email}");
        }

        if (string.IsNullOrEmpty(subject))
        {
            throw new InvalidOperationException("Subject claim not found in OIDC response");
        }

        // Provider key format: "oidc:{providerId}:{sub}"
        // URL-encode components to prevent collision if they contain ':'
        var providerKey = $"oidc:{Uri.EscapeDataString(providerId)}:{Uri.EscapeDataString(subject)}";

        // Try to find existing user by provider key first
        var user = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.GenericOidc, providerKey, cancellationToken);

        if (user is not null)
        {
            return await IssueLoginResult(user, ipAddress, userAgent, cancellationToken);
        }

        // Look up by email — only auto-link if provider confirms email is verified
        user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is not null)
        {
            if (providerConfig.RequireEmailVerifiedForAutoLink)
            {
                var emailVerifiedClaim = claimsPrincipal.FindFirst(providerConfig.EmailVerifiedClaimType)?.Value;
                if (!string.Equals(emailVerifiedClaim, "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"OIDC provider '{providerId}' did not confirm email verification. " +
                        "Cannot auto-link to an existing account.");
                }
            }

            await CreateUserLoginAsync(user.Id, providerId, providerKey, name ?? email, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return await IssueLoginResult(user, ipAddress, userAgent, cancellationToken);
        }

        // Auto-register new user
        var username = await DeriveUniqueUsernameAsync(email, cancellationToken);
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            EmailVerified = true, // OIDC provider already verified
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Generate a random password hash so password login is disabled
        user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

        await _userRepository.AddAsync(user, cancellationToken);
        await CreateUserLoginAsync(user.Id, providerId, providerKey, name ?? email, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await IssueLoginResult(user, ipAddress, userAgent, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task LinkOidcAccountAsync(
        Guid userId,
        string providerId,
        ClaimsPrincipal claimsPrincipal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(providerId))
            throw new ArgumentException("Provider ID cannot be null or empty.", nameof(providerId));

        EnsureGenericOidcEnabled();

        var providerConfig = GetProviderConfig(providerId);
        var (email, name, subject) = ExtractClaims(claimsPrincipal, providerConfig);

        if (string.IsNullOrEmpty(subject))
        {
            throw new InvalidOperationException("Subject claim not found in OIDC response");
        }

        var providerKey = $"oidc:{Uri.EscapeDataString(providerId)}:{Uri.EscapeDataString(subject)}";

        // Check if this provider is linked to another user
        var otherUser = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.GenericOidc, providerKey, cancellationToken);

        if (otherUser is not null && otherUser.Id != userId)
        {
            throw new InvalidOperationException($"OIDC provider '{providerId}' is already linked to another account");
        }

        await CreateUserLoginAsync(userId, providerId, providerKey, name ?? email ?? "OIDC User", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public IEnumerable<OidcProviderConfig> GetConfiguredProviders()
    {
        return _options.OidcProviders ?? Enumerable.Empty<OidcProviderConfig>();
    }

    private void EnsureGenericOidcEnabled()
    {
        if (!_options.EnableGenericOidc)
        {
            throw new InvalidOperationException("Generic OIDC authentication is not enabled");
        }
    }

    private OidcProviderConfig GetProviderConfig(string providerId)
    {
        var config = _options.OidcProviders?.FirstOrDefault(p =>
            p.ProviderId.Equals(providerId, StringComparison.OrdinalIgnoreCase));

        if (config is null)
        {
            throw new InvalidOperationException($"OIDC provider '{providerId}' is not configured");
        }

        return config;
    }

    private (string? email, string? name, string? subject) ExtractClaims(
        ClaimsPrincipal claimsPrincipal,
        OidcProviderConfig providerConfig)
    {
        var email = claimsPrincipal.FindFirst(providerConfig.EmailClaimType)?.Value;
        var name = claimsPrincipal.FindFirst(providerConfig.NameClaimType)?.Value;
        var subject = claimsPrincipal.FindFirst(providerConfig.SubjectClaimType)?.Value;

        return (email, name, subject);
    }

    private async Task CreateUserLoginAsync(
        Guid userId,
        string providerId,
        string providerKey,
        string displayName,
        CancellationToken cancellationToken)
    {
        var userLogin = new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.GenericOidc,
            ProviderKey = providerKey,
            ProviderDisplayName = displayName,
            LinkedAt = DateTime.UtcNow
        };

        await _userRepository.AddLoginAsync(userLogin, cancellationToken);
    }

    private async Task<LoginResult> IssueLoginResult(
        User user,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        // Load roles if not already loaded
        if (user.UserRoles is null)
        {
            var userWithRoles = await _userRepository.GetWithRolesAsync(user.Id, cancellationToken);
            if (userWithRoles is null)
                throw new UserNotFoundException(user.Id);
            user = userWithRoles;
        }

        var roleNames = user.UserRoles?
            .Where(ur => ur.Role is not null)
            .Select(ur => ur.Role!.Name)
            .ToList() ?? new List<string>();

        var permissions = user.UserRoles?
            .Select(ur => ur.Role?.Permissions)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .SelectMany(p => p!.Split(',').Select(x => x.Trim().ToLowerInvariant()))
            .Distinct()
            .ToList() ?? new List<string>();

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

        var sessionId = Guid.NewGuid();
        if (_options.EnableSessionManagement && _sessionService is not null)
        {
            var session = await _sessionService.CreateSessionAsync(
                user.Id, refreshTokenHash, ipAddress, userAgent, cancellationToken);
            sessionId = session.Id;
        }

        var accessToken = permissions.Count == 0
            ? _jwtTokenService.GenerateAccessToken(user, roleNames, sessionId)
            : _jwtTokenService.GenerateAccessToken(user, roleNames, permissions, sessionId);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult
        {
            AccessToken = accessToken.Token,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            SessionId = sessionId,
            RequiresTwoFactor = false,
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

    private async Task<string> DeriveUniqueUsernameAsync(string email, CancellationToken cancellationToken)
    {
        var localPart = email.Split('@')[0].ToLowerInvariant();
        // Keep only alphanumerics and underscores to prevent invalid/injection-prone usernames
        var sanitized = System.Text.RegularExpressions.Regex.Replace(localPart, @"[^a-z0-9_]", "");
        var baseUsername = string.IsNullOrEmpty(sanitized) ? "user" : sanitized;

        var username = baseUsername;
        var counter = 1;

        while (await _userRepository.GetByUsernameAsync(username, cancellationToken) is not null)
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        return username;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}