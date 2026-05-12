using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using FluentAssertions;
using Moq;
using Xunit;

namespace DainnUser.UnitTests.Services;

public class ActivityServiceTests
{
    private readonly Mock<IActivityLogRepository> _mockActivityLogRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly ActivityService _service;

    public ActivityServiceTests()
    {
        _mockActivityLogRepository = new Mock<IActivityLogRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new ActivityService(_mockActivityLogRepository.Object, _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task LogActivityAsync_CreatesActivityLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activityType = ActivityType.Login;
        var description = "User logged in";
        var ipAddress = "192.168.1.1";
        var userAgent = "Mozilla/5.0";
        var metadata = "{\"device\":\"mobile\"}";

        // Act
        await _service.LogActivityAsync(userId, activityType, description, ipAddress, userAgent, metadata);

        // Assert
        _mockActivityLogRepository.Verify(r => r.AddAsync(
            It.Is<ActivityLog>(log =>
                log.UserId == userId &&
                log.ActivityType == activityType &&
                log.Description == description &&
                log.IpAddress == ipAddress &&
                log.UserAgent == userAgent &&
                log.Metadata == metadata),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogActivityAsync_WithMinimalData_CreatesActivityLog()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var activityType = ActivityType.ProfileUpdate;

        // Act
        await _service.LogActivityAsync(userId, activityType);

        // Assert
        _mockActivityLogRepository.Verify(r => r.AddAsync(
            It.Is<ActivityLog>(log =>
                log.UserId == userId &&
                log.ActivityType == activityType &&
                log.Description == null &&
                log.IpAddress == null &&
                log.UserAgent == null &&
                log.Metadata == null),
            It.IsAny<CancellationToken>()), Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithPagination_ReturnsPagedResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<ActivityLog>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ActivityType = ActivityType.Login, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = userId, ActivityType = ActivityType.Logout, CreatedAt = DateTime.UtcNow }
        };

        _mockActivityLogRepository
            .Setup(r => r.GetByUserIdAsync(userId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((logs, 2));

        // Act
        var result = await _service.GetActivityLogAsync(userId, 1, 10);

        // Assert
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithActivityTypeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<ActivityLog>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ActivityType = ActivityType.Login, CreatedAt = DateTime.UtcNow }
        };

        _mockActivityLogRepository
            .Setup(r => r.GetByActivityTypeAsync(userId, ActivityType.Login, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _service.GetActivityLogAsync(userId, 1, 10, ActivityType.Login);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().ActivityType.Should().Be(ActivityType.Login);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithDateRangeFilter_ReturnsFilteredResults()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var logs = new List<ActivityLog>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ActivityType = ActivityType.Login, CreatedAt = DateTime.UtcNow.AddDays(-3) }
        };

        _mockActivityLogRepository
            .Setup(r => r.GetByDateRangeAsync(userId, startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _service.GetActivityLogAsync(userId, 1, 10, null, startDate, endDate);

        // Assert
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetActivityLogAsync_WithInvalidPageNumber_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetActivityLogAsync(userId, 0, 10));
    }

    [Fact]
    public async Task GetActivityLogAsync_WithInvalidPageSize_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetActivityLogAsync(userId, 1, 0));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetActivityLogAsync(userId, 1, 101));
    }

    [Fact]
    public async Task GetRecentActivityAsync_ReturnsRecentLogs()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var logs = new List<ActivityLog>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, ActivityType = ActivityType.Login, CreatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), UserId = userId, ActivityType = ActivityType.ProfileUpdate, CreatedAt = DateTime.UtcNow.AddMinutes(-5) }
        };

        _mockActivityLogRepository
            .Setup(r => r.GetRecentByUserIdAsync(userId, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _service.GetRecentActivityAsync(userId, 10);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRecentActivityAsync_WithInvalidCount_ThrowsArgumentException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetRecentActivityAsync(userId, 0));
        await Assert.ThrowsAsync<ArgumentException>(() => _service.GetRecentActivityAsync(userId, 101));
    }

    [Fact]
    public async Task CleanupOldLogsAsync_RemovesOldLogs()
    {
        // Arrange
        var beforeDate = DateTime.UtcNow.AddDays(-180);
        _mockActivityLogRepository
            .Setup(r => r.RemoveOldLogsAsync(beforeDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        // Act
        var result = await _service.CleanupOldLogsAsync(beforeDate);

        // Assert
        result.Should().Be(50);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
