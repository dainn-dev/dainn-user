using DainnUser.Application.Services;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using DainnUser.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DainnUser.IntegrationTests.Services;

public class ActivityServiceIntegrationTests : IDisposable
{
    private readonly DainnUserDbContext _context;
    private readonly ActivityService _service;
    private readonly IActivityLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivityServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<DainnUserDbContext>()
            .UseInMemoryDatabase(databaseName: $"ActivityServiceTest_{Guid.NewGuid()}")
            .Options;

        _context = new DainnUserDbContext(options);
        _repository = new ActivityLogRepository(_context);
        _unitOfWork = new UnitOfWork(_context);
        _service = new ActivityService(_repository, _unitOfWork);
    }

    [Fact]
    public async Task LogActivityAsync_PersistsToDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activityType = ActivityType.Login;
        var description = "User logged in successfully";
        var ipAddress = "192.168.1.100";
        var userAgent = "Mozilla/5.0";

        // Act
        await _service.LogActivityAsync(userId, activityType, description, ipAddress, userAgent);

        // Assert
        var logs = await _repository.GetByUserIdAsync(userId, 1, 10);
        logs.Items.Should().HaveCount(1);
        var log = logs.Items.First();
        log.UserId.Should().Be(userId);
        log.ActivityType.Should().Be(activityType);
        log.Description.Should().Be(description);
        log.IpAddress.Should().Be(ipAddress);
        log.UserAgent.Should().Be(userAgent);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var userId = Guid.NewGuid();
        for (int i = 0; i < 25; i++)
        {
            await _service.LogActivityAsync(userId, ActivityType.ProfileUpdate, $"Update {i}");
        }

        // Act - Get page 2
        var result = await _service.GetActivityLogAsync(userId, 2, 10);

        // Assert
        result.Items.Should().HaveCount(10);
        result.TotalCount.Should().Be(25);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithActivityTypeFilter_ReturnsOnlyMatchingType()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _service.LogActivityAsync(userId, ActivityType.Login);
        await _service.LogActivityAsync(userId, ActivityType.Login);
        await _service.LogActivityAsync(userId, ActivityType.Logout);
        await _service.LogActivityAsync(userId, ActivityType.ProfileUpdate);

        // Act
        var result = await _service.GetActivityLogAsync(userId, 1, 10, ActivityType.Login);

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(log => log.ActivityType == ActivityType.Login);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithDateRangeFilter_ReturnsOnlyLogsInRange()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Create logs with different timestamps by directly adding to repository
        var oldLog = new Core.Entities.ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = ActivityType.Login,
            CreatedAt = now.AddDays(-10)
        };
        var recentLog = new Core.Entities.ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = ActivityType.Login,
            CreatedAt = now.AddDays(-3)
        };

        await _repository.AddAsync(oldLog);
        await _repository.AddAsync(recentLog);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var startDate = now.AddDays(-7);
        var endDate = now;
        var result = await _service.GetActivityLogAsync(userId, 1, 10, null, startDate, endDate);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().CreatedAt.Should().BeAfter(startDate);
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsLatestLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        for (int i = 0; i < 15; i++)
        {
            await _service.LogActivityAsync(userId, ActivityType.ProfileUpdate, $"Update {i}");
        }

        // Act
        var result = await _service.GetRecentActivityAsync(userId, 5);

        // Assert
        result.Should().HaveCount(5);
    }

    [Fact]
    public async Task CleanupOldLogsAsync_RemovesLogsBeforeDate()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Create old logs
        for (int i = 0; i < 5; i++)
        {
            var oldLog = new Core.Entities.ActivityLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActivityType = ActivityType.Login,
                CreatedAt = now.AddDays(-200)
            };
            await _repository.AddAsync(oldLog);
        }

        // Create recent logs
        for (int i = 0; i < 3; i++)
        {
            await _service.LogActivityAsync(userId, ActivityType.Login);
        }

        await _unitOfWork.SaveChangesAsync();

        // Act
        var cutoffDate = now.AddDays(-180);
        var removedCount = await _service.CleanupOldLogsAsync(cutoffDate);

        // Assert
        removedCount.Should().Be(5);
        var remainingLogs = await _repository.GetByUserIdAsync(userId, 1, 100);
        remainingLogs.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithMultipleFilters_ReturnsCorrectResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        // Create logs with different types and dates
        var log1 = new Core.Entities.ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = ActivityType.Login,
            CreatedAt = now.AddDays(-5)
        };
        var log2 = new Core.Entities.ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = ActivityType.Login,
            CreatedAt = now.AddDays(-3)
        };
        var log3 = new Core.Entities.ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = ActivityType.Logout,
            CreatedAt = now.AddDays(-3)
        };

        await _repository.AddAsync(log1);
        await _repository.AddAsync(log2);
        await _repository.AddAsync(log3);
        await _unitOfWork.SaveChangesAsync();

        // Act - Filter by type AND date range
        var startDate = now.AddDays(-4);
        var endDate = now;
        var result = await _service.GetActivityLogAsync(userId, 1, 10, ActivityType.Login, startDate, endDate);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().ActivityType.Should().Be(ActivityType.Login);
        result.Items.First().CreatedAt.Should().BeAfter(startDate);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
