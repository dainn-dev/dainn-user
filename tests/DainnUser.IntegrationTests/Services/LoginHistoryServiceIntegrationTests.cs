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

public class LoginHistoryServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;
    private readonly LoginHistoryService _loginHistoryService;
    private readonly LoginHistoryRepository _loginHistoryRepository;
    private readonly UnitOfWork _unitOfWork;
    private readonly DainnUserOptions _options;

    public LoginHistoryServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase();

        _loginHistoryRepository = new LoginHistoryRepository(_fixture.DbContext);
        _unitOfWork = new UnitOfWork(_fixture.DbContext);
        _options = new DainnUserOptions
        {
            LoginHistoryRetentionDays = 30
        };

        _loginHistoryService = new LoginHistoryService(
            _loginHistoryRepository,
            _unitOfWork,
            Options.Create(_options));
    }

    [Fact]
    public async Task GetLoginHistoryAsync_WithPaginationAndDateRange_ReturnsFilteredResults()
    {
        // Arrange
        var user = await CreateUserAsync("history@example.com", "historyuser");

        // Create login history records across different dates
        var records = new List<LoginHistory>
        {
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.1",
                UserAgent = "Agent1",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.2",
                UserAgent = "Agent2",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = false,
                IpAddress = "192.168.1.3",
                UserAgent = "Agent3",
                FailureReason = "Invalid password",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Google,
                IsSuccessful = true,
                IpAddress = "192.168.1.4",
                UserAgent = "Agent4",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.5",
                UserAgent = "Agent5",
                CreatedAt = DateTime.UtcNow
            }
        };

        _fixture.DbContext.LoginHistories.AddRange(records);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - Get page 1 with date range filter (last 7 days)
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(1);
        var (items, totalCount) = await _loginHistoryService.GetLoginHistoryAsync(
            user.Id, pageNumber: 1, pageSize: 2, startDate, endDate);

        // Assert - Should return 2 items from page 1, total 4 in range
        items.Should().HaveCount(2);
        totalCount.Should().Be(4); // Records from last 7 days
        items.Should().OnlyContain(h => h.CreatedAt >= startDate && h.CreatedAt <= endDate);

        // Act - Get page 2
        var (itemsPage2, totalCountPage2) = await _loginHistoryService.GetLoginHistoryAsync(
            user.Id, pageNumber: 2, pageSize: 2, startDate, endDate);

        // Assert - Should return remaining 2 items
        itemsPage2.Should().HaveCount(2);
        totalCountPage2.Should().Be(4);

        // Act - Get all records without date filter
        var (allItems, allTotalCount) = await _loginHistoryService.GetLoginHistoryAsync(
            user.Id, pageNumber: 1, pageSize: 10);

        // Assert - Should return all 5 records
        allItems.Should().HaveCount(5);
        allTotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetRecentLoginsAsync_ReturnsLatestRecords()
    {
        // Arrange
        var user = await CreateUserAsync("recent@example.com", "recentuser");

        // Create login history records
        var records = new List<LoginHistory>();
        for (int i = 0; i < 10; i++)
        {
            records.Add(new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = $"192.168.1.{i}",
                UserAgent = $"Agent{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i * 10)
            });
        }

        _fixture.DbContext.LoginHistories.AddRange(records);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - Get 5 most recent logins
        var recentLogins = await _loginHistoryService.GetRecentLoginsAsync(user.Id, count: 5);

        // Assert - Should return 5 most recent records
        recentLogins.Should().HaveCount(5);
        recentLogins.Should().BeInDescendingOrder(h => h.CreatedAt);
        recentLogins.First().IpAddress.Should().Be("192.168.1.0"); // Most recent
        recentLogins.Last().IpAddress.Should().Be("192.168.1.4"); // 5th most recent
    }

    [Fact]
    public async Task GetFailedAttemptsAsync_ReturnsOnlyFailedAttemptsSinceDate()
    {
        // Arrange
        var user = await CreateUserAsync("failed@example.com", "faileduser");

        // Create mix of successful and failed login attempts
        var records = new List<LoginHistory>
        {
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = false,
                IpAddress = "192.168.1.1",
                UserAgent = "Agent1",
                FailureReason = "Invalid password",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = false,
                IpAddress = "192.168.1.2",
                UserAgent = "Agent2",
                FailureReason = "Account locked",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.3",
                UserAgent = "Agent3",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = false,
                IpAddress = "192.168.1.4",
                UserAgent = "Agent4",
                FailureReason = "Invalid credentials",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = false,
                IpAddress = "192.168.1.5",
                UserAgent = "Agent5",
                FailureReason = "User not found",
                CreatedAt = DateTime.UtcNow
            }
        };

        _fixture.DbContext.LoginHistories.AddRange(records);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - Get failed attempts since 5 days ago
        var since = DateTime.UtcNow.AddDays(-5);
        var failedAttempts = await _loginHistoryService.GetFailedAttemptsAsync(user.Id, since);

        // Assert - Should return only failed attempts within date range
        failedAttempts.Should().HaveCount(3);
        failedAttempts.Should().OnlyContain(h => !h.IsSuccessful && h.CreatedAt >= since);
        failedAttempts.Should().NotContain(h => h.IpAddress == "192.168.1.1"); // Too old
        failedAttempts.Should().NotContain(h => h.IpAddress == "192.168.1.3"); // Successful
    }

    [Fact]
    public async Task CleanupOldRecordsAsync_RemovesOldRecordsCorrectly()
    {
        // Arrange
        var user = await CreateUserAsync("cleanup@example.com", "cleanupuser");

        // Create records: some old (should be removed), some recent (should stay)
        var oldRecords = new List<LoginHistory>
        {
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.1",
                UserAgent = "OldAgent1",
                CreatedAt = DateTime.UtcNow.AddDays(-40) // Older than retention
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = false,
                IpAddress = "192.168.1.2",
                UserAgent = "OldAgent2",
                FailureReason = "Old failure",
                CreatedAt = DateTime.UtcNow.AddDays(-35) // Older than retention
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.3",
                UserAgent = "OldAgent3",
                CreatedAt = DateTime.UtcNow.AddDays(-31) // Older than retention
            }
        };

        var recentRecords = new List<LoginHistory>
        {
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.4",
                UserAgent = "RecentAgent1",
                CreatedAt = DateTime.UtcNow.AddDays(-20) // Within retention
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.5",
                UserAgent = "RecentAgent2",
                CreatedAt = DateTime.UtcNow.AddDays(-5) // Within retention
            }
        };

        _fixture.DbContext.LoginHistories.AddRange(oldRecords);
        _fixture.DbContext.LoginHistories.AddRange(recentRecords);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Verify initial state: 5 records total
        var allRecordsBefore = await _fixture.DbContext.LoginHistories
            .AsNoTracking()
            .Where(h => h.UserId == user.Id)
            .ToListAsync();
        allRecordsBefore.Should().HaveCount(5);

        // Act - Cleanup old records
        var removedCount = await _loginHistoryService.CleanupOldRecordsAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - 3 old records removed
        removedCount.Should().Be(3);

        // Assert - Only 2 recent records remain
        var allRecordsAfter = await _fixture.DbContext.LoginHistories
            .AsNoTracking()
            .Where(h => h.UserId == user.Id)
            .ToListAsync();
        allRecordsAfter.Should().HaveCount(2);
        allRecordsAfter.Should().OnlyContain(h =>
            h.IpAddress == "192.168.1.4" || h.IpAddress == "192.168.1.5");
    }

    [Fact]
    public async Task CleanupOldRecordsAsync_IsIdempotent_SecondCallReturnsZero()
    {
        // Arrange
        var user = await CreateUserAsync("idempotent@example.com", "idempotentuser");

        // Create only old records
        var oldRecords = new List<LoginHistory>
        {
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.1",
                UserAgent = "OldAgent1",
                CreatedAt = DateTime.UtcNow.AddDays(-40)
            },
            new LoginHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Provider = LoginProvider.Local,
                IsSuccessful = true,
                IpAddress = "192.168.1.2",
                UserAgent = "OldAgent2",
                CreatedAt = DateTime.UtcNow.AddDays(-35)
            }
        };

        _fixture.DbContext.LoginHistories.AddRange(oldRecords);
        await _fixture.DbContext.SaveChangesAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Act - First cleanup
        var firstCleanupCount = await _loginHistoryService.CleanupOldRecordsAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - First cleanup removed records
        firstCleanupCount.Should().Be(2);

        // Act - Second cleanup (should find nothing to remove)
        var secondCleanupCount = await _loginHistoryService.CleanupOldRecordsAsync();
        _fixture.DbContext.ChangeTracker.Clear();

        // Assert - Second cleanup returns 0
        secondCleanupCount.Should().Be(0);

        // Assert - No records remain
        var remainingRecords = await _fixture.DbContext.LoginHistories
            .AsNoTracking()
            .Where(h => h.UserId == user.Id)
            .ToListAsync();
        remainingRecords.Should().BeEmpty();
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
