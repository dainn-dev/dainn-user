# PSA-68 Login History and Audit Trail Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `ILoginHistoryService` / `LoginHistoryService` with paginated, date-filtered query APIs, retention config, and cleanup capability.

**Architecture:** LoginHistoryService depends on ILoginHistoryRepository + DainnUserOptions. Adds 2 new repository methods for date-range filtering and bulk cleanup. Follows existing service/interface/DI patterns.

**Tech Stack:** .NET 8, xUnit, Moq, FluentAssertions

---

## File Map

```
src/DainnUser.Core/Interfaces/Services/
  + ILoginHistoryService.cs         ← new

src/DainnUser.Core/Interfaces/Repositories/
  ILoginHistoryRepository.cs        ← modify (add 2 methods)

src/DainnUser.Infrastructure/Repositories/
  LoginHistoryRepository.cs         ← modify (implement 2 new methods)

src/DainnUser.Application/Services/
  + LoginHistoryService.cs          ← new

src/DainnUser.Application/
  ApplicationServiceExtensions.cs   ← modify (add DI registration)

src/DainnUser.Infrastructure/Configuration/
  DainnUserOptions.cs              ← modify (add LoginHistoryRetentionDays)

tests/DainnUser.UnitTests/Services/
  + LoginHistoryServiceTests.cs    ← new

tests/DainnUser.IntegrationTests/Services/
  + LoginHistoryServiceIntegrationTests.cs ← new
```

---

## Task 1: Add LoginHistoryRetentionDays to DainnUserOptions

**Files:**
- Modify: `src/DainnUser.Infrastructure/Configuration/DainnUserOptions.cs`

Add after `MaxActiveSessionsPerUser` property:

```csharp
/// <summary>
/// Gets or sets the login history retention period in days.
/// Records older than this will be eligible for cleanup.
/// </summary>
public int LoginHistoryRetentionDays { get; set; } = 90;
```

---

## Task 2: Add repository methods for date range and cleanup

**Files:**
- Modify: `src/DainnUser.Core/Interfaces/Repositories/ILoginHistoryRepository.cs`
- Modify: `src/DainnUser.Infrastructure/Repositories/LoginHistoryRepository.cs`

### Add to ILoginHistoryRepository:

```csharp
/// <summary>
/// Gets login history for a specific user with optional date range filtering and pagination.
/// </summary>
Task<(IEnumerable<LoginHistory> Items, int TotalCount)> GetByUserIdWithDateRangeAsync(
    Guid userId,
    int pageNumber,
    int pageSize,
    DateTime? startDate,
    DateTime? endDate,
    CancellationToken cancellationToken = default);

/// <summary>
/// Removes login history records older than the specified cutoff date.
/// </summary>
/// <param name="cutoffDate">Records with CreatedAt older than this date will be removed.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The number of records removed.</returns>
Task<int> RemoveOldRecordsAsync(
    DateTime cutoffDate,
    CancellationToken cancellationToken = default);
```

### Add to LoginHistoryRepository:

```csharp
public async Task<(IEnumerable<LoginHistory> Items, int TotalCount)> GetByUserIdWithDateRangeAsync(
    Guid userId,
    int pageNumber,
    int pageSize,
    DateTime? startDate,
    DateTime? endDate,
    CancellationToken cancellationToken = default)
{
    var query = _dbSet.Where(lh => lh.UserId == userId);

    if (startDate.HasValue)
        query = query.Where(lh => lh.CreatedAt >= startDate.Value);

    if (endDate.HasValue)
        query = query.Where(lh => lh.CreatedAt <= endDate.Value);

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .OrderByDescending(lh => lh.CreatedAt)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync(cancellationToken);

    return (items, totalCount);
}

public async Task<int> RemoveOldRecordsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
{
    var oldRecords = await _dbSet
        .Where(lh => lh.CreatedAt < cutoffDate)
        .ToListAsync(cancellationToken);

    if (oldRecords.Count == 0)
        return 0;

    _dbSet.RemoveRange(oldRecords);
    return oldRecords.Count;
}
```

---

## Task 3: Create ILoginHistoryService interface

**Files:**
- Create: `src/DainnUser.Core/Interfaces/Services/ILoginHistoryService.cs`

```csharp
using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service interface for login history queries and audit.
/// </summary>
public interface ILoginHistoryService
{
    /// <summary>
    /// Gets paginated login history for a user with optional date range filtering.
    /// </summary>
    Task<(IReadOnlyCollection<LoginHistory> Items, int TotalCount)> GetLoginHistoryAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent login attempts for a user.
    /// </summary>
    Task<IReadOnlyCollection<LoginHistory>> GetRecentLoginsAsync(
        Guid userId,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets failed login attempts for a user since a given date.
    /// </summary>
    Task<IReadOnlyCollection<LoginHistory>> GetFailedAttemptsAsync(
        Guid userId,
        DateTime since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes login history records older than the configured retention period.
    /// Returns the number of records removed.
    /// </summary>
    Task<int> CleanupOldRecordsAsync(
        CancellationToken cancellationToken = default);
}
```

---

## Task 4: Create LoginHistoryService implementation

**Files:**
- Create: `src/DainnUser.Application/Services/LoginHistoryService.cs`

```csharp
using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
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
```

---

## Task 5: Register LoginHistoryService in DI

**Files:**
- Modify: `src/DainnUser.Application/ApplicationServiceExtensions.cs`

Add alongside other service registrations:

```csharp
services.AddScoped<ILoginHistoryService, LoginHistoryService>();
```

---

## Task 6: Write unit tests for LoginHistoryService

**Files:**
- Create: `tests/DainnUser.UnitTests/Services/LoginHistoryServiceTests.cs`

Test cases:
1. `GetLoginHistoryAsync` returns paginated results from repository.
2. `GetLoginHistoryAsync` filters by date range when startDate and endDate provided.
3. `GetLoginHistoryAsync` throws ArgumentException when pageNumber < 1.
4. `GetLoginHistoryAsync` throws ArgumentException when pageSize < 1 or > 100.
5. `GetLoginHistoryAsync` throws ArgumentException when startDate > endDate.
6. `GetRecentLoginsAsync` returns items from repository.
7. `GetFailedAttemptsAsync` returns items from repository with correct since date.
8. `CleanupOldRecordsAsync` removes records older than retention period and returns count.
9. `CleanupOldRecordsAsync` returns 0 when no records to clean.

Use Moq for `ILoginHistoryRepository`, `IUnitOfWork`, `IOptions<DainnUserOptions>`. Use FluentAssertions.

---

## Task 7: Write integration tests for LoginHistoryService

**Files:**
- Create: `tests/DainnUser.IntegrationTests/Services/LoginHistoryServiceIntegrationTests.cs`

Use existing `DatabaseFixture`. Follow pattern from `SessionServiceIntegrationTests.cs`.

Test cases:
1. Can query login history with pagination and date range filtering.
2. Can get recent logins.
3. Can get failed attempts.
4. Cleanup removes old records correctly.
5. Cleanup is idempotent (second call returns 0).

---

## Verification

After all tasks:
1. Run: `dotnet build` — must pass with no errors.
2. Run: `dotnet test` — all tests must pass.

---

## Spec Coverage Check

| Requirement | Task |
|---|---|
| `ILoginHistoryService` interface | Task 3 |
| `GetLoginHistoryAsync()` with pagination | Tasks 3, 4 |
| Filter by date range | Tasks 2, 3, 4 |
| `GetRecentLoginsAsync()` | Tasks 3, 4 |
| `GetFailedAttemptsAsync()` | Tasks 3, 4 |
| `CleanupOldRecordsAsync()` | Tasks 3, 4 |
| Retention period config | Task 1 |
| Repository date range + cleanup methods | Task 2 |
| DI registration | Task 5 |
| Unit tests | Task 6 |
| Integration tests | Task 7 |
