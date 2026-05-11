# PSA-68 Login History and Audit Trail Design

## Goal

Implement login history service (`ILoginHistoryService`) to provide query APIs for login history with pagination and date range filtering. Add retention policy configuration and cleanup capability.

## Scope

In scope:

- Add `ILoginHistoryService` with query methods for login history.
- Add `LoginHistoryService` implementation.
- Add date range filtering to login history queries.
- Add `LoginHistoryRetentionDays` configuration option (default: 90 days).
- Add `RemoveOldRecordsAsync` to `ILoginHistoryRepository` for cleanup.
- Unit and integration tests.

Out of scope:

- Suspicious activity detection (`ISuspiciousActivityAnalyzer`) — deferred to future security enhancement work.
- Background job scheduling for cleanup — deferred to future work.
- Alerting on suspicious patterns — deferred to future work.

## Existing Context

The codebase already has:

- `LoginHistory` entity with Id, UserId, Provider, IsSuccessful, IpAddress, UserAgent, FailureReason, CreatedAt.
- `ILoginHistoryRepository` with methods: GetByUserIdAsync (paginated), GetRecentByUserIdAsync, GetFailedAttemptsAsync, GetByProviderAsync, GetByIpAddressAsync.
- `LoginHistoryRepository` implementation.
- `AuthenticationService.RecordLoginAttemptAsync()` already logs all login attempts (successful and failed).

## Service Contract

Create `ILoginHistoryService` in `DainnUser.Core.Interfaces.Services`:

```csharp
Task<(IReadOnlyCollection<LoginHistory> Items, int TotalCount)> GetLoginHistoryAsync(
    Guid userId,
    int pageNumber,
    int pageSize,
    DateTime? startDate,
    DateTime? endDate,
    CancellationToken cancellationToken = default);

Task<IReadOnlyCollection<LoginHistory>> GetRecentLoginsAsync(
    Guid userId,
    int count,
    CancellationToken cancellationToken = default);

Task<IReadOnlyCollection<LoginHistory>> GetFailedAttemptsAsync(
    Guid userId,
    DateTime since,
    CancellationToken cancellationToken = default);

Task<int> CleanupOldRecordsAsync(
    CancellationToken cancellationToken = default);
```

## LoginHistoryService Implementation

`LoginHistoryService` lives in `DainnUser.Application.Services`, depends on `ILoginHistoryRepository` and `DainnUserOptions`.

**Date range filtering:**
- `GetLoginHistoryAsync` accepts optional `startDate` and `endDate` parameters.
- If provided, filter `LoginHistory.CreatedAt` to be within the range.
- Delegate to a new `ILoginHistoryRepository.GetByUserIdWithDateRangeAsync` method.

**Retention cleanup:**
- `CleanupOldRecordsAsync` removes records older than `LoginHistoryRetentionDays` (from `DainnUserOptions`).
- Calls `ILoginHistoryRepository.RemoveOldRecordsAsync(cutoffDate)` where `cutoffDate = DateTime.UtcNow.AddDays(-_options.LoginHistoryRetentionDays)`.
- Returns count of removed records.

**Other methods:**
- `GetRecentLoginsAsync`: delegates to `ILoginHistoryRepository.GetRecentByUserIdAsync`.
- `GetFailedAttemptsAsync`: delegates to `ILoginHistoryRepository.GetFailedAttemptsAsync`.

## Repository Changes

Add to `ILoginHistoryRepository`:

```csharp
Task<(IEnumerable<LoginHistory> Items, int TotalCount)> GetByUserIdWithDateRangeAsync(
    Guid userId,
    int pageNumber,
    int pageSize,
    DateTime? startDate,
    DateTime? endDate,
    CancellationToken cancellationToken = default);

Task<int> RemoveOldRecordsAsync(
    DateTime cutoffDate,
    CancellationToken cancellationToken = default);
```

Implementation in `LoginHistoryRepository`:
- `GetByUserIdWithDateRangeAsync`: query with optional date filters on `CreatedAt`, paginate, return items + total count.
- `RemoveOldRecordsAsync`: delete records where `CreatedAt < cutoffDate`, return count deleted.

## Configuration

Add to `DainnUserOptions`:

```csharp
/// <summary>
/// Gets or sets the login history retention period in days.
/// Records older than this will be eligible for cleanup.
/// </summary>
public int LoginHistoryRetentionDays { get; set; } = 90;
```

## Error Handling

- `GetLoginHistoryAsync` for non-existent user: return empty collection (not an error).
- `CleanupOldRecordsAsync`: idempotent, returns 0 if no records to clean.
- Invalid pagination parameters (pageNumber < 1, pageSize < 1): throw `ArgumentException`.

## Validation

- `pageNumber` must be >= 1.
- `pageSize` must be >= 1 and <= 100 (max page size).
- `startDate` must be <= `endDate` if both provided.

## Testing

Unit tests should cover:

- `GetLoginHistoryAsync` returns paginated results.
- `GetLoginHistoryAsync` filters by date range when provided.
- `GetLoginHistoryAsync` returns empty for non-existent user.
- `GetLoginHistoryAsync` throws for invalid pagination parameters.
- `GetRecentLoginsAsync` returns recent logins.
- `GetFailedAttemptsAsync` returns failed attempts since date.
- `CleanupOldRecordsAsync` removes old records and returns count.

Integration tests should cover:

- Full login history query with pagination and date filtering.
- Cleanup removes records older than retention period.

## Security and Compatibility

- No new authorization decisions (assumes caller has already verified access to userId).
- No breaking changes to existing service contracts.
- No in-memory state.
- Login history is append-only (no updates to existing records).
