using DainnUser.Core.Configuration;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Repositories;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DainnUser.IntegrationTests.Services;

public class SessionServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly SessionService _sessionService;
    private readonly SessionRepository _sessionRepository;
    private readonly UserRepository _userRepository;
    private readonly UnitOfWork _unitOfWork;
    private readonly DainnUserOptions _options;

    public SessionServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase();

        _sessionRepository = new SessionRepository(_fixture.DbContext);
        _userRepository = new UserRepository(_fixture.DbContext);
        _unitOfWork = new UnitOfWork(_fixture.DbContext);
        _options = new DainnUserOptions
        {
            MaxActiveSessionsPerUser = 5,
            RefreshTokenExpirationDays = 7
        };

        _sessionService = new SessionService(
            _sessionRepository,
            _userRepository,
            _unitOfWork,
            Options.Create(_options));
    }

    [Fact]
    public async Task FullSessionLifecycle_CreateGetRevokeVerify()
    {
        // Arrange
        var user = await CreateUserAsync("lifecycle@example.com", "lifecycleuser");
        var refreshTokenHash = "hash-lifecycle-token";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0 Test Browser";

        // Act - Create session
        var session = await _sessionService.CreateSessionAsync(
            user.Id, refreshTokenHash, ipAddress, userAgent);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - Session created
        session.Should().NotBeNull();
        session.Id.Should().NotBeEmpty();
        session.UserId.Should().Be(user.Id);
        session.SessionToken.Should().Be(refreshTokenHash);
        session.IpAddress.Should().Be(ipAddress);
        session.UserAgent.Should().Be(userAgent);
        session.IsActive.Should().BeTrue();
        session.ExpiresAt.Should().BeCloseTo(
            DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays),
            TimeSpan.FromSeconds(5));

        // Act - Get active sessions
        var activeSessions = await _sessionService.GetActiveSessionsAsync(user.Id);

        // Assert - Session is present
        activeSessions.Should().ContainSingle();
        activeSessions.First().Id.Should().Be(session.Id);
        activeSessions.First().IsActive.Should().BeTrue();

        // Act - Revoke session
        await _sessionService.RevokeSessionAsync(session.Id);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - Session is inactive
        var activeSessionsAfterRevoke = await _sessionService.GetActiveSessionsAsync(user.Id);
        activeSessionsAfterRevoke.Should().BeEmpty();

        var sessionInDb = await _fixture.DbContext.UserSessions
            .AsNoTracking()
            .FirstAsync(s => s.Id == session.Id);
        sessionInDb.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task MaxSessionsEnforcement_CreatesOnlyMaxAllowed_OldestEvicted()
    {
        // Arrange
        var user = await CreateUserAsync("maxsessions@example.com", "maxsessionsuser");
        _options.MaxActiveSessionsPerUser = 5;

        // Act - Create 6 sessions
        var sessionIds = new List<Guid>();
        for (int i = 0; i < 6; i++)
        {
            var session = await _sessionService.CreateSessionAsync(
                user.Id,
                $"hash-token-{i}",
                $"192.168.1.{i}",
                $"UserAgent-{i}");
            sessionIds.Add(session.Id);

            // Small delay to ensure different LastActivityAt timestamps
            await Task.Delay(50);
            _fixture.DbContext.ChangeTracker.Clear();
        }

        // Assert - Only 5 sessions are active
        var activeSessions = await _sessionService.GetActiveSessionsAsync(user.Id);
        activeSessions.Should().HaveCount(5);

        // Assert - First (oldest) session was evicted
        var allSessionsInDb = await _fixture.DbContext.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync();

        allSessionsInDb.Should().HaveCount(6);
        allSessionsInDb[0].Id.Should().Be(sessionIds[0]);
        allSessionsInDb[0].IsActive.Should().BeFalse(); // Oldest evicted

        // Assert - Last 5 sessions are active
        for (int i = 1; i < 6; i++)
        {
            allSessionsInDb[i].IsActive.Should().BeTrue();
        }
    }

    [Fact]
    public async Task RotateSessionAsync_WorksEndToEnd_NewHashStoredOldHashGone()
    {
        // Arrange
        var user = await CreateUserAsync("rotate@example.com", "rotateuser");
        var oldTokenHash = "old-hash-token";
        var newTokenHash = "new-hash-token";
        var oldIpAddress = "10.0.0.1";
        var newIpAddress = "10.0.0.2";
        var oldUserAgent = "OldAgent/1.0";
        var newUserAgent = "NewAgent/2.0";

        var session = await _sessionService.CreateSessionAsync(
            user.Id, oldTokenHash, oldIpAddress, oldUserAgent);
        _fixture.DbContext.ChangeTracker.Clear();

        var originalExpiresAt = session.ExpiresAt;

        // Small delay to ensure LastActivityAt changes
        await Task.Delay(100);

        // Act - Rotate session
        var rotatedSession = await _sessionService.RotateSessionAsync(
            oldTokenHash, newTokenHash, newIpAddress, newUserAgent);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - Rotation succeeded
        rotatedSession.Should().NotBeNull();
        rotatedSession!.Id.Should().Be(session.Id);
        rotatedSession.SessionToken.Should().Be(newTokenHash);
        rotatedSession.IpAddress.Should().Be(newIpAddress);
        rotatedSession.UserAgent.Should().Be(newUserAgent);
        rotatedSession.IsActive.Should().BeTrue();
        rotatedSession.LastActivityAt.Should().BeAfter(session.LastActivityAt);
        rotatedSession.ExpiresAt.Should().BeAfter(originalExpiresAt);

        // Assert - New hash is stored in database
        var sessionInDb = await _fixture.DbContext.UserSessions
            .AsNoTracking()
            .FirstAsync(s => s.Id == session.Id);
        sessionInDb.SessionToken.Should().Be(newTokenHash);
        sessionInDb.IpAddress.Should().Be(newIpAddress);
        sessionInDb.UserAgent.Should().Be(newUserAgent);

        // Assert - Old hash no longer works
        var rotateWithOldHash = await _sessionService.RotateSessionAsync(
            oldTokenHash, "another-new-hash", null, null);
        rotateWithOldHash.Should().BeNull();
    }

    [Fact]
    public async Task RevokeAllSessionsAsync_DeactivatesAllForUser()
    {
        // Arrange
        var user = await CreateUserAsync("revokeall@example.com", "revokealluser");

        // Create 3 sessions
        var session1 = await _sessionService.CreateSessionAsync(
            user.Id, "hash-1", "1.1.1.1", "Agent1");
        var session2 = await _sessionService.CreateSessionAsync(
            user.Id, "hash-2", "2.2.2.2", "Agent2");
        var session3 = await _sessionService.CreateSessionAsync(
            user.Id, "hash-3", "3.3.3.3", "Agent3");
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify all are active
        var activeBeforeRevoke = await _sessionService.GetActiveSessionsAsync(user.Id);
        activeBeforeRevoke.Should().HaveCount(3);

        // Act - Revoke all sessions
        await _sessionService.RevokeAllSessionsAsync(user.Id);
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - No active sessions
        var activeAfterRevoke = await _sessionService.GetActiveSessionsAsync(user.Id);
        activeAfterRevoke.Should().BeEmpty();

        // Assert - All sessions are inactive in database
        var allSessionsInDb = await _fixture.DbContext.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .ToListAsync();

        allSessionsInDb.Should().HaveCount(3);
        allSessionsInDb.Should().OnlyContain(s => !s.IsActive);
    }

    [Fact]
    public async Task CleanupExpiredSessionsAsync_RemovesExpiredSessions()
    {
        // Arrange
        var user = await CreateUserAsync("cleanup@example.com", "cleanupuser");

        // Create 2 active sessions
        var activeSession1 = await _sessionService.CreateSessionAsync(
            user.Id, "hash-active-1", "10.0.0.1", "ActiveAgent1");
        var activeSession2 = await _sessionService.CreateSessionAsync(
            user.Id, "hash-active-2", "10.0.0.2", "ActiveAgent2");

        // Create 3 expired sessions manually
        var expiredSession1 = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = "hash-expired-1",
            IpAddress = "10.0.0.10",
            UserAgent = "ExpiredAgent1",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(-3),
            LastActivityAt = DateTime.UtcNow.AddDays(-3),
            IsActive = true
        };

        var expiredSession2 = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = "hash-expired-2",
            IpAddress = "10.0.0.11",
            UserAgent = "ExpiredAgent2",
            CreatedAt = DateTime.UtcNow.AddDays(-8),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            LastActivityAt = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        };

        var expiredSession3 = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            SessionToken = "hash-expired-3",
            IpAddress = "10.0.0.12",
            UserAgent = "ExpiredAgent3",
            CreatedAt = DateTime.UtcNow.AddDays(-15),
            ExpiresAt = DateTime.UtcNow.AddHours(-1),
            LastActivityAt = DateTime.UtcNow.AddHours(-1),
            IsActive = false // Already inactive
        };

        _fixture.DbContext.UserSessions.AddRange(
            expiredSession1, expiredSession2, expiredSession3);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify initial state: 5 sessions total
        var allSessionsBefore = await _fixture.DbContext.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .ToListAsync();
        allSessionsBefore.Should().HaveCount(5);

        // Act - Cleanup expired sessions
        var removedCount = await _sessionService.CleanupExpiredSessionsAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - 3 expired sessions removed
        removedCount.Should().Be(3);

        // Assert - Only 2 active sessions remain
        var allSessionsAfter = await _fixture.DbContext.UserSessions
            .AsNoTracking()
            .Where(s => s.UserId == user.Id)
            .ToListAsync();
        allSessionsAfter.Should().HaveCount(2);
        allSessionsAfter.Should().Contain(s => s.Id == activeSession1.Id);
        allSessionsAfter.Should().Contain(s => s.Id == activeSession2.Id);
        allSessionsAfter.Should().NotContain(s => s.Id == expiredSession1.Id);
        allSessionsAfter.Should().NotContain(s => s.Id == expiredSession2.Id);
        allSessionsAfter.Should().NotContain(s => s.Id == expiredSession3.Id);
    }

    private async Task<User> CreateUserAsync(string email, string username)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            Status = UserStatus.Active,
            EmailVerified = true,
            PasswordHash = "dummy-hash",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();
        return user;
    }
}
