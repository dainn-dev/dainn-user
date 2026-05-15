using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using DainnUser.Core.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for social login providers.
/// </summary>
public class SocialLoginService : ISocialLoginService
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ISessionService? _sessionService;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly DainnUserOptions _options;
    private readonly HttpClient _httpClient;

    public SocialLoginService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IJwtTokenService jwtTokenService,
        ISessionService? sessionService,
        IPasswordHasher<User> passwordHasher,
        IOptions<DainnUserOptions> options,
        HttpClient httpClient)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
        _sessionService = sessionService;
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _httpClient = httpClient;
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginWithGoogleAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var (accessToken, _) = await ExchangeCodeForTokensAsync(authorizationCode, callbackUrl, ct);
        var googleUser = await FetchGoogleUserInfoAsync(accessToken, ct);

        // Try to find existing user by provider key first
        var user = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Google, googleUser.Id, ct);

        if (user is not null)
        {
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        // Look up by email
        user = await _userRepository.GetByEmailAsync(googleUser.Email, ct);
        if (user is not null)
        {
            // Link Google account to existing user
            await CreateUserLoginAsync(user.Id, googleUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        // Auto-register new user
        var username = await DeriveUniqueUsernameAsync(googleUser, ct);
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = googleUser.Email,
            Username = username,
            EmailVerified = true, // Google already verified
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Generate a random password hash so password login is disabled
        user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

        await _userRepository.AddAsync(user, ct);
        await CreateUserLoginAsync(user.Id, googleUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await IssueLoginResult(user, ipAddress, userAgent, ct);
    }

    /// <inheritdoc/>
    public async Task LinkGoogleAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        var (accessToken, _) = await ExchangeCodeForTokensAsync(authorizationCode, callbackUrl, ct);
        var googleUser = await FetchGoogleUserInfoAsync(accessToken, ct);

        // Check if this Google account is already linked to someone else
        var existingUser = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Google, googleUser.Id, ct);
        if (existingUser is not null)
        {
            if (existingUser.Id != userId)
                throw new InvalidOperationException("Google account is already linked to another user.");
            return; // Already linked to this user
        }

        await CreateUserLoginAsync(userId, googleUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginWithFacebookAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var accessToken = await ExchangeCodeForFacebookTokensAsync(authorizationCode, callbackUrl, ct);
        var facebookUser = await FetchFacebookUserInfoAsync(accessToken, ct);

        var user = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Facebook, facebookUser.Id, ct);

        if (user is not null)
        {
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        user = await _userRepository.GetByEmailAsync(facebookUser.Email, ct);
        if (user is not null)
        {
            await CreateFacebookUserLoginAsync(user.Id, facebookUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        var username = await DeriveUniqueUsernameFromFacebookAsync(facebookUser, ct);
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = facebookUser.Email,
            Username = username,
            EmailVerified = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

        await _userRepository.AddAsync(user, ct);
        await CreateFacebookUserLoginAsync(user.Id, facebookUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await IssueLoginResult(user, ipAddress, userAgent, ct);
    }

    /// <inheritdoc/>
    public async Task LinkFacebookAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        var accessToken = await ExchangeCodeForFacebookTokensAsync(authorizationCode, callbackUrl, ct);
        var facebookUser = await FetchFacebookUserInfoAsync(accessToken, ct);

        var existingUser = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Facebook, facebookUser.Id, ct);
        if (existingUser is not null)
        {
            if (existingUser.Id != userId)
                throw new InvalidOperationException("Facebook account is already linked to another user.");
            return;
        }

        await CreateFacebookUserLoginAsync(userId, facebookUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginWithGitHubAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var accessToken = await ExchangeCodeForGitHubTokensAsync(authorizationCode, callbackUrl, ct);
        var githubUser = await FetchGitHubUserInfoAsync(accessToken, ct);

        var user = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.GitHub, githubUser.Id, ct);

        if (user is not null)
        {
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        user = await _userRepository.GetByEmailAsync(githubUser.Email, ct);
        if (user is not null)
        {
            await CreateGitHubUserLoginAsync(user.Id, githubUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        var username = await DeriveUniqueUsernameFromGitHubAsync(githubUser, ct);
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = githubUser.Email,
            Username = username,
            EmailVerified = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

        await _userRepository.AddAsync(user, ct);
        await CreateGitHubUserLoginAsync(user.Id, githubUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await IssueLoginResult(user, ipAddress, userAgent, ct);
    }

    /// <inheritdoc/>
    public async Task LinkGitHubAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        var accessToken = await ExchangeCodeForGitHubTokensAsync(authorizationCode, callbackUrl, ct);
        var githubUser = await FetchGitHubUserInfoAsync(accessToken, ct);

        var existingUser = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.GitHub, githubUser.Id, ct);
        if (existingUser is not null)
        {
            if (existingUser.Id != userId)
                throw new InvalidOperationException("GitHub account is already linked to another user.");
            return;
        }

        await CreateGitHubUserLoginAsync(userId, githubUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<LoginResult> LoginWithMicrosoftAsync(
        string authorizationCode,
        string callbackUrl,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var accessToken = await ExchangeCodeForMicrosoftTokensAsync(authorizationCode, callbackUrl, ct);
        var microsoftUser = await FetchMicrosoftUserInfoAsync(accessToken, ct);

        var user = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Microsoft, microsoftUser.Id, ct);

        if (user is not null)
        {
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        user = await _userRepository.GetByEmailAsync(microsoftUser.Email, ct);
        if (user is not null)
        {
            await CreateMicrosoftUserLoginAsync(user.Id, microsoftUser, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return await IssueLoginResult(user, ipAddress, userAgent, ct);
        }

        var username = await DeriveUniqueUsernameFromMicrosoftAsync(microsoftUser, ct);
        user = new User
        {
            Id = Guid.NewGuid(),
            Email = microsoftUser.Email,
            Username = username,
            EmailVerified = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N"));

        await _userRepository.AddAsync(user, ct);
        await CreateMicrosoftUserLoginAsync(user.Id, microsoftUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return await IssueLoginResult(user, ipAddress, userAgent, ct);
    }

    /// <inheritdoc/>
    public async Task LinkMicrosoftAccountAsync(
        Guid userId,
        string authorizationCode,
        string callbackUrl,
        CancellationToken ct = default)
    {
        EnsureSocialLoginEnabled();

        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        var accessToken = await ExchangeCodeForMicrosoftTokensAsync(authorizationCode, callbackUrl, ct);
        var microsoftUser = await FetchMicrosoftUserInfoAsync(accessToken, ct);

        var existingUser = await _userRepository.GetByExternalLoginAsync(
            LoginProvider.Microsoft, microsoftUser.Id, ct);
        if (existingUser is not null)
        {
            if (existingUser.Id != userId)
                throw new InvalidOperationException("Microsoft account is already linked to another user.");
            return;
        }

        await CreateMicrosoftUserLoginAsync(userId, microsoftUser, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task UnlinkProviderAsync(
        Guid userId,
        LoginProvider provider,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        // Load logins if not already loaded
        if (user.Logins is null)
        {
            user = await _userRepository.GetByExternalLoginAsync(provider, string.Empty, ct);
            if (user is null || user.Id != userId)
            {
                user = await _userRepository.GetByIdAsync(userId, ct);
            }
        }

        var login = user!.Logins?.FirstOrDefault(l => l.Provider == provider);
        if (login is null)
            return; // Already unlinked

        // Don't allow unlinking if it's the only login method
        var hasPassword = !string.IsNullOrWhiteSpace(user.PasswordHash);
        var externalLoginCount = user.Logins?.Count(l => l.Provider != provider) ?? 0;

        if (!hasPassword && externalLoginCount == 0)
            throw new InvalidOperationException("Cannot unlink the only login method.");

        user.Logins!.Remove(login);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // --- helpers ---

    private void EnsureSocialLoginEnabled()
    {
        if (!_options.EnableSocialLogin)
            throw new InvalidOperationException("Social login is not enabled.");
    }

    private async Task<(string AccessToken, string RefreshToken)> ExchangeCodeForTokensAsync(
        string code, string callbackUrl, CancellationToken ct)
    {
        var payload = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.GoogleClientId,
            ["client_secret"] = _options.GoogleClientSecret,
            ["redirect_uri"] = callbackUrl,
            ["grant_type"] = "authorization_code"
        };

        var response = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(payload), ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Invalid Google authorization code.");

        var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(
            cancellationToken: ct);

        if (tokenResponse?.AccessToken is null)
            throw new InvalidOperationException("Invalid Google authorization code.");

        return (tokenResponse.AccessToken, tokenResponse.RefreshToken ?? string.Empty);
    }

    private async Task<GoogleUserInfo> FetchGoogleUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetAsync(
            "https://www.googleapis.com/oauth2/v3/userinfo", ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch Google user information.");

        var userInfo = await response.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken: ct);
        return userInfo ?? throw new InvalidOperationException("Failed to parse Google user information.");
    }

    private async Task CreateUserLoginAsync(Guid userId, GoogleUserInfo googleUser, CancellationToken ct)
    {
        await _unitOfWork.Users.AddLoginAsync(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Google,
            ProviderKey = googleUser.Id,
            ProviderDisplayName = googleUser.Name,
            LinkedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task<LoginResult> IssueLoginResult(
        User user, string? ipAddress, string? userAgent, CancellationToken ct)
    {
        // Load roles if not already loaded
        if (user.UserRoles is null)
        {
            var userWithRoles = await _userRepository.GetWithRolesAsync(user.Id, ct);
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
        }, ct);

        var sessionId = Guid.NewGuid();
        if (_options.EnableSessionManagement && _sessionService is not null)
        {
            var session = await _sessionService.CreateSessionAsync(
                user.Id, refreshTokenHash, ipAddress, userAgent, ct);
            sessionId = session.Id;
        }

        var accessToken = permissions.Count == 0
            ? _jwtTokenService.GenerateAccessToken(user, roleNames, sessionId)
            : _jwtTokenService.GenerateAccessToken(user, roleNames, permissions, sessionId);

        await _unitOfWork.SaveChangesAsync(ct);

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

    private async Task<string> DeriveUniqueUsernameAsync(GoogleUserInfo googleUser, CancellationToken ct)
    {
        // Use email prefix as base
        var prefix = googleUser.Email.Split('@')[0];
        var username = prefix;

        // Check if username is taken, append random suffix if needed
        var isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
        if (isTaken)
        {
            // Append random 4-digit suffix
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                username = $"{prefix}{random.Next(1000, 9999)}";
                isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
                if (!isTaken)
                    break;
            }

            // If still taken after 10 attempts, use GUID suffix
            if (isTaken)
            {
                username = $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
            }
        }

        return username;
    }

    private async Task<string> ExchangeCodeForFacebookTokensAsync(
        string code, string callbackUrl, CancellationToken ct)
    {
        var url = $"https://graph.facebook.com/v18.0/oauth/access_token?client_id={Uri.EscapeDataString(_options.FacebookAppId)}&client_secret={Uri.EscapeDataString(_options.FacebookAppSecret)}&redirect_uri={Uri.EscapeDataString(callbackUrl)}&code={Uri.EscapeDataString(code)}";

        var response = await _httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Invalid Facebook authorization code.");

        var tokenResponse = await response.Content.ReadFromJsonAsync<FacebookTokenResponse>(
            cancellationToken: ct);

        if (tokenResponse?.AccessToken is null)
            throw new InvalidOperationException("Invalid Facebook authorization code.");

        return tokenResponse.AccessToken;
    }

    private async Task<FacebookUserInfo> FetchFacebookUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(
            $"https://graph.facebook.com/v18.0/me?fields=id,email,name,picture&access_token={accessToken}", ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch Facebook user information.");

        var userInfo = await response.Content.ReadFromJsonAsync<FacebookUserInfo>(cancellationToken: ct);
        return userInfo ?? throw new InvalidOperationException("Failed to parse Facebook user information.");
    }

    private async Task CreateFacebookUserLoginAsync(Guid userId, FacebookUserInfo facebookUser, CancellationToken ct)
    {
        await _unitOfWork.Users.AddLoginAsync(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Facebook,
            ProviderKey = facebookUser.Id,
            ProviderDisplayName = facebookUser.Name,
            LinkedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task<string> DeriveUniqueUsernameFromFacebookAsync(FacebookUserInfo facebookUser, CancellationToken ct)
    {
        var prefix = facebookUser.Email.Split('@')[0];
        var username = prefix;

        var isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
        if (isTaken)
        {
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                username = $"{prefix}{random.Next(1000, 9999)}";
                isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
                if (!isTaken)
                    break;
            }

            if (isTaken)
            {
                username = $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
            }
        }

        return username;
    }

    private async Task<string> ExchangeCodeForGitHubTokensAsync(
        string code, string callbackUrl, CancellationToken ct)
    {
        var payload = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.GitHubClientId,
            ["client_secret"] = _options.GitHubClientSecret,
            ["redirect_uri"] = callbackUrl
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(payload)
        };
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Invalid GitHub authorization code.");

        var tokenResponse = await response.Content.ReadFromJsonAsync<GitHubTokenResponse>(
            cancellationToken: ct);

        if (tokenResponse?.AccessToken is null)
            throw new InvalidOperationException("Invalid GitHub authorization code.");

        return tokenResponse.AccessToken;
    }

    private async Task<GitHubUserInfo> FetchGitHubUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
        request.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("DainnUser", "1.0"));

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch GitHub user information.");

        var userInfo = await response.Content.ReadFromJsonAsync<GitHubUserInfo>(cancellationToken: ct);
        if (userInfo is null)
            throw new InvalidOperationException("Failed to parse GitHub user information.");

        // If email is null, fetch from emails endpoint
        if (string.IsNullOrWhiteSpace(userInfo.Email))
        {
            var emailRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            emailRequest.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            emailRequest.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("DainnUser", "1.0"));

            var emailResponse = await _httpClient.SendAsync(emailRequest, ct);
            if (emailResponse.IsSuccessStatusCode)
            {
                var emails = await emailResponse.Content.ReadFromJsonAsync<List<GitHubEmail>>(cancellationToken: ct);
                var primaryEmail = emails?.FirstOrDefault(e => e.Primary && e.Verified);
                if (primaryEmail is not null)
                {
                    userInfo.Email = primaryEmail.EmailAddress;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(userInfo.Email))
            throw new InvalidOperationException("GitHub account does not have a verified email address.");

        return userInfo;
    }

    private async Task CreateGitHubUserLoginAsync(Guid userId, GitHubUserInfo githubUser, CancellationToken ct)
    {
        await _unitOfWork.Users.AddLoginAsync(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.GitHub,
            ProviderKey = githubUser.Id,
            ProviderDisplayName = githubUser.Name ?? githubUser.Login,
            LinkedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task<string> DeriveUniqueUsernameFromGitHubAsync(GitHubUserInfo githubUser, CancellationToken ct)
    {
        // Use GitHub login as base, or email prefix if login is not available
        var prefix = !string.IsNullOrWhiteSpace(githubUser.Login)
            ? githubUser.Login
            : githubUser.Email.Split('@')[0];
        var username = prefix;

        var isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
        if (isTaken)
        {
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                username = $"{prefix}{random.Next(1000, 9999)}";
                isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
                if (!isTaken)
                    break;
            }

            if (isTaken)
            {
                username = $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
            }
        }

        return username;
    }

    private async Task<string> ExchangeCodeForMicrosoftTokensAsync(
        string code, string callbackUrl, CancellationToken ct)
    {
        var payload = new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = _options.MicrosoftClientId,
            ["client_secret"] = _options.MicrosoftClientSecret,
            ["redirect_uri"] = callbackUrl,
            ["grant_type"] = "authorization_code"
        };

        var response = await _httpClient.PostAsync(
            "https://login.microsoftonline.com/common/oauth2/v2.0/token",
            new FormUrlEncodedContent(payload), ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Invalid Microsoft authorization code.");

        var tokenResponse = await response.Content.ReadFromJsonAsync<MicrosoftTokenResponse>(
            cancellationToken: ct);

        if (tokenResponse?.AccessToken is null)
            throw new InvalidOperationException("Invalid Microsoft authorization code.");

        return tokenResponse.AccessToken;
    }

    private async Task<MicrosoftUserInfo> FetchMicrosoftUserInfoAsync(
        string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch Microsoft user information.");

        var userInfo = await response.Content.ReadFromJsonAsync<MicrosoftUserInfo>(cancellationToken: ct);
        if (userInfo is null)
            throw new InvalidOperationException("Failed to parse Microsoft user information.");

        if (string.IsNullOrWhiteSpace(userInfo.Mail) && string.IsNullOrWhiteSpace(userInfo.UserPrincipalName))
            throw new InvalidOperationException("Microsoft account does not have an email address.");

        // Use Mail if available, otherwise UserPrincipalName
        userInfo.Email = userInfo.Mail ?? userInfo.UserPrincipalName!;

        return userInfo;
    }

    private async Task CreateMicrosoftUserLoginAsync(Guid userId, MicrosoftUserInfo microsoftUser, CancellationToken ct)
    {
        await _unitOfWork.Users.AddLoginAsync(new UserLogin
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Microsoft,
            ProviderKey = microsoftUser.Id,
            ProviderDisplayName = microsoftUser.DisplayName ?? microsoftUser.Email,
            LinkedAt = DateTime.UtcNow
        }, ct);
    }

    private async Task<string> DeriveUniqueUsernameFromMicrosoftAsync(MicrosoftUserInfo microsoftUser, CancellationToken ct)
    {
        var prefix = microsoftUser.Email.Split('@')[0];
        var username = prefix;

        var isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
        if (isTaken)
        {
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                username = $"{prefix}{random.Next(1000, 9999)}";
                isTaken = await _userRepository.IsUsernameTakenAsync(username, null, ct);
                if (!isTaken)
                    break;
            }

            if (isTaken)
            {
                username = $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
            }
        }

        return username;
    }

    // --- Google API models ---

    private class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    private class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public string? Picture { get; set; }
    }

    // --- Facebook API models ---

    private class FacebookTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
    }

    private class FacebookUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("picture")]
        public FacebookPicture? Picture { get; set; }
    }

    private class FacebookPicture
    {
        [JsonPropertyName("data")]
        public FacebookPictureData? Data { get; set; }
    }

    private class FacebookPictureData
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    // --- GitHub API models ---

    private class GitHubTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private class GitHubUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("login")]
        public string? Login { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("avatar_url")]
        public string? AvatarUrl { get; set; }
    }

    private class GitHubEmail
    {
        [JsonPropertyName("email")]
        public string EmailAddress { get; set; } = string.Empty;

        [JsonPropertyName("primary")]
        public bool Primary { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }
    }

    // --- Microsoft API models ---

    private class MicrosoftTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }

    private class MicrosoftUserInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("mail")]
        public string? Mail { get; set; }

        [JsonPropertyName("userPrincipalName")]
        public string? UserPrincipalName { get; set; }

        public string Email { get; set; } = string.Empty;
    }
}
