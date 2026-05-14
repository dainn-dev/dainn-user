using DainnUser.Core.Entities;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Configuration;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for user session lifecycle management.
/// </summary>
public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DainnUserOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionService"/> class.
    /// </summary>
    public SessionService(
        ISessionRepository sessionRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOptions<DainnUserOptions> options)
    {
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<UserSession> CreateSessionAsync(
        Guid userId,
        string refreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        // Enforce max active sessions per user
        if (_options.MaxActiveSessionsPerUser > 0)
        {
            var activeSessions = (await _sessionRepository.GetActiveByUserIdAsync(userId, ct))
                .OrderBy(s => s.LastActivityAt)
                .ToList();

            while (activeSessions.Count >= _options.MaxActiveSessionsPerUser)
            {
                var oldest = activeSessions[0];
                oldest.IsActive = false;
                oldest.LastActivityAt = DateTime.UtcNow;
                activeSessions.RemoveAt(0);
            }
        }

        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = refreshTokenHash,
            IpAddress = Normalize(ipAddress),
            UserAgent = Normalize(userAgent),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        await _sessionRepository.AddAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return session;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<UserSession>> GetActiveSessionsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetActiveByUserIdAsync(userId, ct);
        return sessions.ToList();
    }

    /// <inheritdoc/>
    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is not null && session.IsActive)
        {
            session.IsActive = false;
            session.LastActivityAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetActiveByUserIdAsync(userId, ct);
        foreach (var s in sessions)
        {
            s.IsActive = false;
            s.LastActivityAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task RevokeAllExceptAsync(
        Guid userId, Guid keepSessionId, CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetActiveByUserIdAsync(userId, ct);
        foreach (var s in sessions.Where(s => s.Id != keepSessionId))
        {
            s.IsActive = false;
            s.LastActivityAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    /// <inheritdoc/>
    public async Task<UserSession?> RotateSessionAsync(
        string oldRefreshTokenHash,
        string newRefreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByTokenAsync(oldRefreshTokenHash, ct);
        if (session is null || !session.IsActive)
            return null;

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            session.IsActive = false;
            await _unitOfWork.SaveChangesAsync(ct);
            return null;
        }

        session.SessionToken = newRefreshTokenHash;
        session.LastActivityAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        if (!string.IsNullOrWhiteSpace(ipAddress))
            session.IpAddress = Normalize(ipAddress);
        if (!string.IsNullOrWhiteSpace(userAgent))
            session.UserAgent = Normalize(userAgent);

        await _unitOfWork.SaveChangesAsync(ct);
        return session;
    }

    /// <inheritdoc/>
    public async Task UpdateLastActivityAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is not null && session.IsActive)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    /// <inheritdoc/>
    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken ct = default)
    {
        var count = await _sessionRepository.RemoveExpiredSessionsAsync(ct);
        if (count > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        return count;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
