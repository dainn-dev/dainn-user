using System.Security.Cryptography;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    /// <param name="emailService">The email service.</param>
    /// <param name="passwordHasher">The password hasher.</param>
    public AuthenticationService(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _passwordHasher = passwordHasher;
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

        // Generate email verification token
        var verificationToken = GenerateSecureToken();
        var token = new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.EmailVerification,
            TokenValue = verificationToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        user.Tokens.Add(token);

        // Save user to database
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send verification email
        await _emailService.SendEmailVerificationAsync(email, username, verificationToken, cancellationToken);

        return user.Id;
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

        // Find valid verification token
        var verificationToken = user.Tokens.FirstOrDefault(t =>
            t.TokenType == TokenType.EmailVerification &&
            t.TokenValue == token &&
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

        // Generate new verification token
        var verificationToken = GenerateSecureToken();
        var token = new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.EmailVerification,
            TokenValue = verificationToken,
            ExpiresAt = DateTime.UtcNow.AddHours(24),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        user.Tokens.Add(token);
        user.UpdatedAt = DateTime.UtcNow;

        // No need to call Update() - entity is already tracked and EF Core will detect changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send verification email
        await _emailService.SendEmailVerificationAsync(email, user.Username, verificationToken, cancellationToken);

        return true;
    }

    /// <summary>
    /// Generates a cryptographically secure random token.
    /// </summary>
    /// <returns>A base64-encoded secure token.</returns>
    private static string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
