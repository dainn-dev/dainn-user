using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Authentication;
using DainnUser.Core.Configuration;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for TOTP-based two-factor authentication.
/// </summary>
public class TwoFactorService : ITwoFactorService
{
    private const int TotpStepSeconds = 30;
    private const int TotpDigits = 6;
    private const int TotpAllowedWindow = 1;
    private const int BackupCodeCount = 10;
    private const int BackupCodeLength = 8;

    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DainnUserOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TwoFactorService"/> class.
    /// </summary>
    public TwoFactorService(IUserRepository userRepository, IUnitOfWork unitOfWork, DainnUserOptions options)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    /// <inheritdoc/>
    public async Task<TwoFactorSetupResult> PrepareEnableAsync(
        Guid userId,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (!_options.EnableTwoFactor)
        {
            throw new InvalidOperationException("Two-factor authentication is disabled.");
        }

        var user = await _userRepository.GetByIdWithTokensAsync(userId, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.TwoFactorEnabled)
        {
            throw new InvalidOperationException("Two-factor authentication is already enabled.");
        }

        foreach (var existing in user.Tokens.Where(t => t.TokenType == TokenType.TwoFactorSecret && !t.IsUsed && !t.IsRevoked))
        {
            existing.IsRevoked = true;
            existing.RevokedAt = DateTime.UtcNow;
        }

        var secret = GenerateBase32Secret();
        await _userRepository.AddTokenAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.TwoFactorSecret,
            TokenValue = secret,
            ExpiresAt = DateTime.UtcNow.AddMinutes(_options.TwoFactorSetupExpirationMinutes),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TwoFactorSetupResult
        {
            Secret = secret,
            OtpAuthUri = BuildOtpAuthUri(userEmail, secret)
        };
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> EnableTwoFactorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithTokensAsync(userId, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        if (user.TwoFactorEnabled)
        {
            throw new InvalidOperationException("Two-factor authentication is already enabled.");
        }

        var pendingSecret = user.Tokens
            .Where(t => t.TokenType == TokenType.TwoFactorSecret && !t.IsUsed && !t.IsRevoked && t.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("No pending two-factor setup found.");

        if (!VerifyTotp(pendingSecret.TokenValue, code))
        {
            throw new InvalidTwoFactorCodeException();
        }

        user.TwoFactorEnabled = true;
        user.TwoFactorSecret = pendingSecret.TokenValue;
        user.UpdatedAt = DateTime.UtcNow;
        pendingSecret.IsUsed = true;
        pendingSecret.UsedAt = DateTime.UtcNow;

        var backupCodes = await GenerateBackupCodesAsync(user.Id, cancellationToken);
        await AddActivityAsync(user.Id, ActivityType.TwoFactorEnabled, "Two-factor authentication enabled.", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return backupCodes;
    }

    /// <inheritdoc/>
    public async Task DisableTwoFactorAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithTokensAsync(userId, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            return;
        }

        if (!VerifyTotp(user.TwoFactorSecret, code) && !TryUseBackupCode(user, code))
        {
            throw new InvalidTwoFactorCodeException();
        }

        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.UpdatedAt = DateTime.UtcNow;

        RevokeTokens(user, TokenType.TwoFactorBackupCode);
        RevokeTokens(user, TokenType.TwoFactorRememberDevice);
        await AddActivityAsync(user.Id, ActivityType.TwoFactorDisabled, "Two-factor authentication disabled.", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<string?> VerifyTwoFactorCodeAsync(
        Guid userId,
        string code,
        bool rememberDevice,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithTokensAsync(userId, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            return null;
        }

        if (!VerifyTotp(user.TwoFactorSecret, code) && !TryUseBackupCode(user, code))
        {
            throw new InvalidTwoFactorCodeException();
        }

        string? rememberToken = null;
        if (rememberDevice)
        {
            rememberToken = GenerateSecureToken();
            await _userRepository.AddTokenAsync(new UserToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenType = TokenType.TwoFactorRememberDevice,
                TokenValue = HashToken(rememberToken),
                ExpiresAt = DateTime.UtcNow.AddDays(_options.TwoFactorRememberDeviceDays),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return rememberToken;
    }

    /// <inheritdoc/>
    public async Task<bool> IsDeviceTrustedAsync(Guid userId, string deviceToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceToken))
        {
            return false;
        }

        var user = await _userRepository.GetByIdWithTokensAsync(userId, cancellationToken);
        if (user is null || !user.TwoFactorEnabled)
        {
            return false;
        }

        var hash = HashToken(deviceToken.Trim());
        return user.Tokens.Any(t =>
            t.TokenType == TokenType.TwoFactorRememberDevice &&
            t.TokenValue == hash &&
            !t.IsUsed &&
            !t.IsRevoked &&
            t.ExpiresAt > DateTime.UtcNow);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> RegenerateBackupCodesAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdWithTokensAsync(userId, cancellationToken)
                   ?? throw new InvalidOperationException("User not found.");

        if (!user.TwoFactorEnabled || string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            throw new InvalidOperationException("Two-factor authentication is not enabled.");
        }

        if (!VerifyTotp(user.TwoFactorSecret, code))
        {
            throw new InvalidTwoFactorCodeException();
        }

        RevokeTokens(user, TokenType.TwoFactorBackupCode);
        var backupCodes = await GenerateBackupCodesAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return backupCodes;
    }

    private async Task<IReadOnlyList<string>> GenerateBackupCodesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var codes = Enumerable.Range(0, BackupCodeCount)
            .Select(_ => GenerateBackupCode())
            .ToList();

        foreach (var code in codes)
        {
            await _userRepository.AddTokenAsync(new UserToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenType = TokenType.TwoFactorBackupCode,
                TokenValue = HashToken(code),
                ExpiresAt = DateTime.UtcNow.AddYears(10),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }

        return codes;
    }

    private bool TryUseBackupCode(User user, string code)
    {
        var normalized = NormalizeBackupCode(code);
        if (normalized.Length != BackupCodeLength)
        {
            return false;
        }

        var hash = HashToken(normalized);
        var token = user.Tokens.FirstOrDefault(t =>
            t.TokenType == TokenType.TwoFactorBackupCode &&
            t.TokenValue == hash &&
            !t.IsUsed &&
            !t.IsRevoked &&
            t.ExpiresAt > DateTime.UtcNow);

        if (token is null)
        {
            return false;
        }

        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;
        return true;
    }

    private void RevokeTokens(User user, TokenType tokenType)
    {
        foreach (var token in user.Tokens.Where(t => t.TokenType == tokenType && !t.IsRevoked))
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    private async Task AddActivityAsync(
        Guid userId,
        ActivityType activityType,
        string description,
        CancellationToken cancellationToken)
    {
        if (!_options.EnableActivityLogging)
        {
            return;
        }

        await _unitOfWork.ActivityLogs.AddAsync(new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = activityType,
            Description = description,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private static bool VerifyTotp(string base32Secret, string code)
    {
        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != TotpDigits)
        {
            return false;
        }

        var secretBytes = DecodeBase32(base32Secret);
        var currentCounter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / TotpStepSeconds;

        for (var offset = -TotpAllowedWindow; offset <= TotpAllowedWindow; offset++)
        {
            var expected = GenerateTotp(secretBytes, currentCounter + offset);
            if (FixedTimeEquals(expected, normalizedCode))
            {
                return true;
            }
        }

        return false;
    }

    private static string GenerateTotp(byte[] secret, long counter)
    {
        var counterBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(counter));
        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | ((hash[offset + 1] & 0xff) << 16)
                     | ((hash[offset + 2] & 0xff) << 8)
                     | (hash[offset + 3] & 0xff);
        var otp = binary % (int)Math.Pow(10, TotpDigits);
        return otp.ToString(new string('0', TotpDigits), CultureInfo.InvariantCulture);
    }

    private static string GenerateBase32Secret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return EncodeBase32(bytes);
    }

    private static string GenerateBackupCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<byte> bytes = stackalloc byte[BackupCodeLength];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[BackupCodeLength];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[bytes[i] % alphabet.Length];
        }

        return new string(chars);
    }

    private static string GenerateSecureToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeBackupCode(string code)
    {
        return new string(code.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private static string BuildOtpAuthUri(string email, string secret)
    {
        const string issuer = "DainnUser";
        var label = Uri.EscapeDataString($"{issuer}:{email}");
        var issuerParam = Uri.EscapeDataString(issuer);
        return $"otpauth://totp/{label}?secret={secret}&issuer={issuerParam}&digits={TotpDigits}&period={TotpStepSeconds}";
    }

    private static string EncodeBase32(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((data.Length + 4) / 5 * 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return output.ToString();
    }

    private static byte[] DecodeBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = input.Trim().TrimEnd('=').Replace(" ", string.Empty).ToUpperInvariant();
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in clean)
        {
            var value = alphabet.IndexOf(c);
            if (value < 0)
            {
                throw new FormatException("Invalid Base32 character.");
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                bitsLeft -= 8;
            }
        }

        return output.ToArray();
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));
    }
}
