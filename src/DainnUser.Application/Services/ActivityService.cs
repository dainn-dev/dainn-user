using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Activity;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for activity logging.
/// </summary>
public class ActivityService : IActivityService
{
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivityService(IActivityLogRepository activityLogRepository, IUnitOfWork unitOfWork)
    {
        _activityLogRepository = activityLogRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogActivityAsync(
        Guid userId,
        ActivityType activityType,
        string? description = null,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        var activityLog = new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = activityType,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow
        };

        await _activityLogRepository.AddAsync(activityLog, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IEnumerable<ActivityLogDto> Items, int TotalCount)> GetActivityLogAsync(
        Guid userId,
        int pageNumber = 1,
        int pageSize = 50,
        ActivityType? activityType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentException("Page number must be greater than 0.", nameof(pageNumber));
        }

        if (pageSize < 1 || pageSize > 100)
        {
            throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));
        }

        IEnumerable<ActivityLog> logs;
        int totalCount;

        if (activityType.HasValue && startDate.HasValue && endDate.HasValue)
        {
            var allLogs = await _activityLogRepository.GetByDateRangeAsync(userId, startDate.Value, endDate.Value, cancellationToken);
            logs = allLogs.Where(l => l.ActivityType == activityType.Value);
            totalCount = logs.Count();
            logs = logs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
        else if (activityType.HasValue)
        {
            var allLogs = await _activityLogRepository.GetByActivityTypeAsync(userId, activityType.Value, cancellationToken);
            totalCount = allLogs.Count();
            logs = allLogs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
        else if (startDate.HasValue && endDate.HasValue)
        {
            var allLogs = await _activityLogRepository.GetByDateRangeAsync(userId, startDate.Value, endDate.Value, cancellationToken);
            totalCount = allLogs.Count();
            logs = allLogs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
        else
        {
            var result = await _activityLogRepository.GetByUserIdAsync(userId, pageNumber, pageSize, cancellationToken);
            logs = result.Items;
            totalCount = result.TotalCount;
        }

        var dtos = logs.Select(MapToDto);
        return (dtos, totalCount);
    }

    public async Task<IEnumerable<ActivityLogDto>> GetRecentActivityAsync(
        Guid userId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        if (count < 1 || count > 100)
        {
            throw new ArgumentException("Count must be between 1 and 100.", nameof(count));
        }

        var logs = await _activityLogRepository.GetRecentByUserIdAsync(userId, count, cancellationToken);
        return logs.Select(MapToDto);
    }

    public async Task<int> CleanupOldLogsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        var count = await _activityLogRepository.RemoveOldLogsAsync(beforeDate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return count;
    }

    private static ActivityLogDto MapToDto(ActivityLog log)
    {
        return new ActivityLogDto
        {
            Id = log.Id,
            UserId = log.UserId,
            ActivityType = log.ActivityType,
            Description = log.Description,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            Metadata = log.Metadata,
            CreatedAt = log.CreatedAt
        };
    }
}
