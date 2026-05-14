using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Configuration;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for login history queries and audit trail.
/// </summary>
public class LoginHistoryService : ILoginHistoryService
{
    private readonly ILoginHistoryRepository _loginHistoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DainnUserOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginHistoryService"/> class.
    /// </summary>
    public LoginHistoryService(
        ILoginHistoryRepository loginHistoryRepository,
        IUnitOfWork unitOfWork,
        IOptions<DainnUserOptions> options)
    {
        _loginHistoryRepository = loginHistoryRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public async Task<(IReadOnlyCollection<LoginHistory> Items, int TotalCount)> GetLoginHistoryAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken ct = default)
    {
        if (pageNumber < 1)
            throw new ArgumentException("Page number must be at least 1.", nameof(pageNumber));

        if (pageSize < 1 || pageSize > 100)
            throw new ArgumentException("Page size must be between 1 and 100.", nameof(pageSize));

        if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            throw new ArgumentException("Start date must not be after end date.", nameof(startDate));

        var (items, totalCount) = await _loginHistoryRepository.GetByUserIdWithDateRangeAsync(
            userId, pageNumber, pageSize, startDate, endDate, ct);

        return (items.ToList(), totalCount);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<LoginHistory>> GetRecentLoginsAsync(
        Guid userId,
        int count,
        CancellationToken ct = default)
    {
        var items = await _loginHistoryRepository.GetRecentByUserIdAsync(userId, count, ct);
        return items.ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyCollection<LoginHistory>> GetFailedAttemptsAsync(
        Guid userId,
        DateTime since,
        CancellationToken ct = default)
    {
        var items = await _loginHistoryRepository.GetFailedAttemptsAsync(userId, since, ct);
        return items.ToList();
    }

    /// <inheritdoc/>
    public async Task<int> CleanupOldRecordsAsync(CancellationToken ct = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_options.LoginHistoryRetentionDays);
        var count = await _loginHistoryRepository.RemoveOldRecordsAsync(cutoffDate, ct);

        if (count > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        return count;
    }
}
