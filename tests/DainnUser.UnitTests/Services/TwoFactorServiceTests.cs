using DainnUser.Core.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;
using Moq;

namespace DainnUser.UnitTests.Services;

public class TwoFactorServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IActivityLogRepository> _activityLogRepositoryMock = new();
    private readonly DainnUserOptions _options = new()
    {
        EnableTwoFactor = true,
        EnableActivityLogging = true,
        TwoFactorSetupExpirationMinutes = 10,
        TwoFactorRememberDeviceDays = 30
    };

    private readonly TwoFactorService _service;

    public TwoFactorServiceTests()
    {
        _unitOfWorkMock.SetupGet(x => x.ActivityLogs).Returns(_activityLogRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _service = new TwoFactorService(_userRepositoryMock.Object, _unitOfWorkMock.Object, _options);
    }

    [Fact]
    public async Task PrepareEnableAsync_WhenTwoFactorDisabled_Throws()
    {
        _options.EnableTwoFactor = false;

        var act = () => _service.PrepareEnableAsync(Guid.NewGuid(), "user@example.com");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Two-factor authentication is disabled.");
    }

    [Fact]
    public async Task PrepareEnableAsync_WithValidUser_CreatesPendingSecretToken()
    {
        var user = CreateUser();
        UserToken? addedToken = null;
        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(user.Id, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Callback<UserToken, CancellationToken>((token, _) => addedToken = token)
            .Returns(Task.CompletedTask);

        var result = await _service.PrepareEnableAsync(user.Id, user.Email);

        result.Secret.Should().NotBeNullOrWhiteSpace();
        result.OtpAuthUri.Should().StartWith("otpauth://totp/");
        addedToken.Should().NotBeNull();
        addedToken!.TokenType.Should().Be(TokenType.TwoFactorSecret);
        addedToken.TokenValue.Should().Be(result.Secret);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_WithValidCode_EnablesUserAndReturnsBackupCodes()
    {
        var user = CreateUser();
        var secret = "JBSWY3DPEHPK3PXP";
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.TwoFactorSecret,
            TokenValue = secret,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });
        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(user.Id, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default)).Returns(Task.CompletedTask);

        var codes = await _service.EnableTwoFactorAsync(user.Id, GenerateTotp(secret));

        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorSecret.Should().Be(secret);
        codes.Should().HaveCount(10);
        _userRepositoryMock.Verify(x => x.AddTokenAsync(It.Is<UserToken>(t =>
            t.TokenType == TokenType.TwoFactorBackupCode && t.TokenValue != codes[0]), default), Times.Exactly(10));
        _activityLogRepositoryMock.Verify(x => x.AddAsync(It.Is<ActivityLog>(l =>
            l.ActivityType == ActivityType.TwoFactorEnabled), default), Times.Once);
    }

    [Fact]
    public async Task EnableTwoFactorAsync_WithInvalidCode_Throws()
    {
        var user = CreateUser();
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.TwoFactorSecret,
            TokenValue = "JBSWY3DPEHPK3PXP",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            CreatedAt = DateTime.UtcNow
        });
        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(user.Id, default)).ReturnsAsync(user);

        var act = () => _service.EnableTwoFactorAsync(user.Id, "000000");

        await act.Should().ThrowAsync<InvalidTwoFactorCodeException>();
    }

    [Fact]
    public async Task VerifyTwoFactorCodeAsync_WithRememberDevice_CreatesTrustedDeviceToken()
    {
        var user = CreateUser();
        user.TwoFactorEnabled = true;
        user.TwoFactorSecret = "JBSWY3DPEHPK3PXP";
        UserToken? rememberToken = null;
        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(user.Id, default)).ReturnsAsync(user);
        _userRepositoryMock.Setup(x => x.AddTokenAsync(It.IsAny<UserToken>(), default))
            .Callback<UserToken, CancellationToken>((token, _) => rememberToken = token)
            .Returns(Task.CompletedTask);

        var token = await _service.VerifyTwoFactorCodeAsync(user.Id, GenerateTotp(user.TwoFactorSecret), true);

        token.Should().NotBeNullOrWhiteSpace();
        rememberToken.Should().NotBeNull();
        rememberToken!.TokenType.Should().Be(TokenType.TwoFactorRememberDevice);
        rememberToken.TokenValue.Should().NotBe(token);
    }

    [Fact]
    public async Task IsDeviceTrustedAsync_WithMatchingRememberToken_ReturnsTrue()
    {
        var user = CreateUser();
        user.TwoFactorEnabled = true;
        var plainToken = "trusted-device-token";
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.TwoFactorRememberDevice,
            TokenValue = HashToken(plainToken),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(user.Id, default)).ReturnsAsync(user);

        var trusted = await _service.IsDeviceTrustedAsync(user.Id, plainToken);

        trusted.Should().BeTrue();
    }

    [Fact]
    public async Task DisableTwoFactorAsync_WithValidBackupCode_DisablesAndRevokesTokens()
    {
        var user = CreateUser();
        var backupCode = "ABCD2345";
        user.TwoFactorEnabled = true;
        user.TwoFactorSecret = "JBSWY3DPEHPK3PXP";
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.TwoFactorBackupCode,
            TokenValue = HashToken(backupCode),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        user.Tokens.Add(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenType = TokenType.TwoFactorRememberDevice,
            TokenValue = HashToken("remember"),
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        });
        _userRepositoryMock.Setup(x => x.GetByIdWithTokensAsync(user.Id, default)).ReturnsAsync(user);

        await _service.DisableTwoFactorAsync(user.Id, backupCode);

        user.TwoFactorEnabled.Should().BeFalse();
        user.TwoFactorSecret.Should().BeNull();
        user.Tokens.Where(t => t.TokenType is TokenType.TwoFactorBackupCode or TokenType.TwoFactorRememberDevice)
            .Should().OnlyContain(t => t.IsRevoked);
    }

    private static User CreateUser()
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            Username = "user",
            Status = UserStatus.Active,
            EmailVerified = true
        };
    }

    private static string GenerateTotp(string base32Secret)
    {
        var secretBytes = DecodeBase32(base32Secret);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var counterBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(counter));
        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | ((hash[offset + 1] & 0xff) << 16)
                     | ((hash[offset + 2] & 0xff) << 8)
                     | (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6");
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

    private static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token.Trim()));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
