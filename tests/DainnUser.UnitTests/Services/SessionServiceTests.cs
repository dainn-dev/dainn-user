using DainnUser.Core.Configuration;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace DainnUser.UnitTests.Services;

public class SessionServiceTests
{
    private readonly Mock<ISessionRepository> _sessionRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private SessionService CreateService(DainnUserOptions? options = null)
    {
        var opts = Options.Create(options ?? new DainnUserOptions
        {
            MaxActiveSessionsPerUser = 5,
            RefreshTokenExpirationDays = 7
        });
        return new SessionService(
            _sessionRepositoryMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object,
            opts);
    }

    #region CreateSessionAsync

    [Fact]
    public async Task CreateSessionAsync_CreatesSessionWithCorrectFields()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var refreshTokenHash = "hash123";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(userId, default))
            .ReturnsAsync(Enumerable.Empty<UserSession>());
        _sessionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserSession>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        var result = await service.CreateSessionAsync(userId, refreshTokenHash, ipAddress, userAgent);

        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.SessionToken.Should().Be(refreshTokenHash);
        result.IpAddress.Should().Be(ipAddress);
        result.UserAgent.Should().Be(userAgent);
        result.IsActive.Should().BeTrue();
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        _sessionRepositoryMock.Verify(x => x.AddAsync(It.Is<UserSession>(s =>
            s.UserId == userId &&
            s.SessionToken == refreshTokenHash &&
            s.IpAddress == ipAddress &&
            s.UserAgent == userAgent &&
            s.IsActive), default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateSessionAsync_ThrowsUserNotFoundException_WhenUserDoesNotExist()
    {
        var userId = Guid.NewGuid();
        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync((User?)null);

        var service = CreateService();
        var act = () => service.CreateSessionAsync(userId, "hash", null, null);

        await act.Should().ThrowAsync<UserNotFoundException>()
            .WithMessage($"*{userId}*");
    }

    [Fact]
    public async Task CreateSessionAsync_DeactivatesOldestSession_WhenAtMaxLimit()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var options = new DainnUserOptions
        {
            MaxActiveSessionsPerUser = 2,
            RefreshTokenExpirationDays = 7
        };

        var oldest = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = "oldest",
            IsActive = true,
            LastActivityAt = DateTime.UtcNow.AddHours(-3)
        };
        var newer = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = "newer",
            IsActive = true,
            LastActivityAt = DateTime.UtcNow.AddHours(-1)
        };
        var activeSessions = new List<UserSession> { oldest, newer };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(userId, default))
            .ReturnsAsync(activeSessions);
        _sessionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserSession>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService(options);
        var result = await service.CreateSessionAsync(userId, "newHash", null, null);

        oldest.IsActive.Should().BeFalse();
        result.IsActive.Should().BeTrue();
        result.SessionToken.Should().Be("newHash");
    }

    [Fact]
    public async Task CreateSessionAsync_DoesNotDeactivateSessions_WhenUnderLimit()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var existing = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = "existing",
            IsActive = true,
            LastActivityAt = DateTime.UtcNow
        };
        var options = new DainnUserOptions
        {
            MaxActiveSessionsPerUser = 5,
            RefreshTokenExpirationDays = 7
        };

        _userRepositoryMock.Setup(x => x.GetByIdAsync(userId, default))
            .ReturnsAsync(user);
        _sessionRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(userId, default))
            .ReturnsAsync(new List<UserSession> { existing });
        _sessionRepositoryMock.Setup(x => x.AddAsync(It.IsAny<UserSession>(), default))
            .Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService(options);
        var result = await service.CreateSessionAsync(userId, "newHash", null, null);

        existing.IsActive.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.SessionToken.Should().Be("newHash");
    }

    #endregion

    #region GetActiveSessionsAsync

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsOnlyActiveNonExpiredSessions()
    {
        var userId = Guid.NewGuid();
        var session1 = new UserSession { Id = Guid.NewGuid(), UserId = userId, IsActive = true };
        var session2 = new UserSession { Id = Guid.NewGuid(), UserId = userId, IsActive = true };

        _sessionRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(userId, default))
            .ReturnsAsync(new List<UserSession> { session1, session2 });

        var service = CreateService();
        var result = await service.GetActiveSessionsAsync(userId);

        result.Should().HaveCount(2);
        result.Should().Contain(new[] { session1, session2 });
    }

    #endregion

    #region RevokeSessionAsync

    [Fact]
    public async Task RevokeSessionAsync_MarksSessionInactive()
    {
        var sessionId = Guid.NewGuid();
        var session = new UserSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            IsActive = true,
            LastActivityAt = DateTime.UtcNow
        };

        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, default))
            .ReturnsAsync(session);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        await service.RevokeSessionAsync(sessionId);

        session.IsActive.Should().BeFalse();
        session.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RevokeSessionAsync_IsIdempotent_WhenSessionDoesNotExist()
    {
        var sessionId = Guid.NewGuid();
        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, default))
            .ReturnsAsync((UserSession?)null);

        var service = CreateService();
        var act = () => service.RevokeSessionAsync(sessionId);

        await act.Should().NotThrowAsync();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    #endregion

    #region RevokeAllSessionsAsync

    [Fact]
    public async Task RevokeAllSessionsAsync_DeactivatesAllUserSessions()
    {
        var userId = Guid.NewGuid();
        var session1 = new UserSession { Id = Guid.NewGuid(), UserId = userId, IsActive = true };
        var session2 = new UserSession { Id = Guid.NewGuid(), UserId = userId, IsActive = true };

        _sessionRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(userId, default))
            .ReturnsAsync(new List<UserSession> { session1, session2 });
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        await service.RevokeAllSessionsAsync(userId);

        session1.IsActive.Should().BeFalse();
        session2.IsActive.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    #endregion

    #region RevokeAllExceptAsync

    [Fact]
    public async Task RevokeAllExceptAsync_PreservesSpecifiedSession()
    {
        var userId = Guid.NewGuid();
        var keepSessionId = Guid.NewGuid();
        var sessionToKeep = new UserSession
        {
            Id = keepSessionId,
            UserId = userId,
            IsActive = true,
            LastActivityAt = DateTime.UtcNow
        };
        var sessionToRevoke = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsActive = true,
            LastActivityAt = DateTime.UtcNow
        };

        _sessionRepositoryMock.Setup(x => x.GetActiveByUserIdAsync(userId, default))
            .ReturnsAsync(new List<UserSession> { sessionToKeep, sessionToRevoke });
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        await service.RevokeAllExceptAsync(userId, keepSessionId);

        sessionToKeep.IsActive.Should().BeTrue();
        sessionToRevoke.IsActive.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    #endregion

    #region RotateSessionAsync

    [Fact]
    public async Task RotateSessionAsync_UpdatesHashAndExtendsExpiry()
    {
        var oldHash = "oldHash";
        var newHash = "newHash";
        var ipAddress = "10.0.0.1";
        var userAgent = "TestAgent/1.0";
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionToken = oldHash,
            IpAddress = null,
            UserAgent = null,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(5),
            LastActivityAt = DateTime.UtcNow
        };

        _sessionRepositoryMock.Setup(x => x.GetByTokenAsync(oldHash, default))
            .ReturnsAsync(session);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        var result = await service.RotateSessionAsync(oldHash, newHash, ipAddress, userAgent);

        result.Should().NotBeNull();
        result!.SessionToken.Should().Be(newHash);
        result.IpAddress.Should().Be(ipAddress);
        result.UserAgent.Should().Be(userAgent);
        result.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        result.LastActivityAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task RotateSessionAsync_ReturnsNull_WhenSessionNotFound()
    {
        var oldHash = "nonexistent";
        _sessionRepositoryMock.Setup(x => x.GetByTokenAsync(oldHash, default))
            .ReturnsAsync((UserSession?)null);

        var service = CreateService();
        var result = await service.RotateSessionAsync(oldHash, "newHash", null, null);

        result.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task RotateSessionAsync_DeactivatesAndReturnsNull_WhenSessionExpired()
    {
        var oldHash = "expiredHash";
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            SessionToken = oldHash,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        _sessionRepositoryMock.Setup(x => x.GetByTokenAsync(oldHash, default))
            .ReturnsAsync(session);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        var result = await service.RotateSessionAsync(oldHash, "newHash", null, null);

        result.Should().BeNull();
        session.IsActive.Should().BeFalse();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    #endregion

    #region UpdateLastActivityAsync

    [Fact]
    public async Task UpdateLastActivityAsync_StampsLastActivityAt()
    {
        var sessionId = Guid.NewGuid();
        var before = DateTime.UtcNow;
        var session = new UserSession
        {
            Id = sessionId,
            UserId = Guid.NewGuid(),
            IsActive = true,
            LastActivityAt = before.AddMinutes(-5)
        };

        _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId, default))
            .ReturnsAsync(session);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        await service.UpdateLastActivityAsync(sessionId);

        session.LastActivityAt.Should().BeOnOrAfter(before);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    #endregion

    #region CleanupExpiredSessionsAsync

    [Fact]
    public async Task CleanupExpiredSessionsAsync_ReturnsCountRemoved()
    {
        _sessionRepositoryMock.Setup(x => x.RemoveExpiredSessionsAsync(default))
            .ReturnsAsync(3);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService();
        var result = await service.CleanupExpiredSessionsAsync();

        result.Should().Be(3);
        _sessionRepositoryMock.Verify(x => x.RemoveExpiredSessionsAsync(default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    #endregion
}
