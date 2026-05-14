using DainnUser.Core.Configuration;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace DainnUser.UnitTests.Services;

public class LoginHistoryServiceTests
{
    private readonly Mock<ILoginHistoryRepository> _loginHistoryRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private LoginHistoryService CreateService(DainnUserOptions? options = null)
    {
        var opts = Options.Create(options ?? new DainnUserOptions
        {
            LoginHistoryRetentionDays = 90
        });
        return new LoginHistoryService(
            _loginHistoryRepositoryMock.Object,
            _unitOfWorkMock.Object,
            opts);
    }

    #region GetLoginHistoryAsync

    [Fact]
    public async Task GetLoginHistoryAsync_ReturnsPaginatedResultsFromRepository()
    {
        var userId = Guid.NewGuid();
        var pageNumber = 1;
        var pageSize = 10;
        var loginHistory1 = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Local,
            IsSuccessful = true,
            IpAddress = "192.168.1.1",
            CreatedAt = DateTime.UtcNow
        };
        var loginHistory2 = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Local,
            IsSuccessful = false,
            IpAddress = "192.168.1.2",
            FailureReason = "Invalid password",
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };
        var items = new List<LoginHistory> { loginHistory1, loginHistory2 };
        var totalCount = 25;

        _loginHistoryRepositoryMock.Setup(x => x.GetByUserIdWithDateRangeAsync(
                userId, pageNumber, pageSize, null, null, default))
            .ReturnsAsync((items, totalCount));

        var service = CreateService();
        var result = await service.GetLoginHistoryAsync(userId, pageNumber, pageSize);

        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(new[] { loginHistory1, loginHistory2 });
        result.TotalCount.Should().Be(totalCount);
        _loginHistoryRepositoryMock.Verify(x => x.GetByUserIdWithDateRangeAsync(
            userId, pageNumber, pageSize, null, null, default), Times.Once);
    }

    [Fact]
    public async Task GetLoginHistoryAsync_FiltersByDateRange_WhenStartDateAndEndDateProvided()
    {
        var userId = Guid.NewGuid();
        var pageNumber = 1;
        var pageSize = 20;
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow;
        var loginHistory = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Local,
            IsSuccessful = true,
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        var items = new List<LoginHistory> { loginHistory };
        var totalCount = 5;

        _loginHistoryRepositoryMock.Setup(x => x.GetByUserIdWithDateRangeAsync(
                userId, pageNumber, pageSize, startDate, endDate, default))
            .ReturnsAsync((items, totalCount));

        var service = CreateService();
        var result = await service.GetLoginHistoryAsync(userId, pageNumber, pageSize, startDate, endDate);

        result.Items.Should().HaveCount(1);
        result.Items.Should().Contain(loginHistory);
        result.TotalCount.Should().Be(totalCount);
        _loginHistoryRepositoryMock.Verify(x => x.GetByUserIdWithDateRangeAsync(
            userId, pageNumber, pageSize, startDate, endDate, default), Times.Once);
    }

    [Fact]
    public async Task GetLoginHistoryAsync_ThrowsArgumentException_WhenPageNumberLessThan1()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        var act = () => service.GetLoginHistoryAsync(userId, 0, 10);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Page number must be at least 1.*")
            .WithParameterName("pageNumber");
    }

    [Fact]
    public async Task GetLoginHistoryAsync_ThrowsArgumentException_WhenPageSizeLessThan1()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        var act = () => service.GetLoginHistoryAsync(userId, 1, 0);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Page size must be between 1 and 100.*")
            .WithParameterName("pageSize");
    }

    [Fact]
    public async Task GetLoginHistoryAsync_ThrowsArgumentException_WhenPageSizeGreaterThan100()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        var act = () => service.GetLoginHistoryAsync(userId, 1, 101);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Page size must be between 1 and 100.*")
            .WithParameterName("pageSize");
    }

    [Fact]
    public async Task GetLoginHistoryAsync_ThrowsArgumentException_WhenStartDateAfterEndDate()
    {
        var userId = Guid.NewGuid();
        var startDate = DateTime.UtcNow;
        var endDate = DateTime.UtcNow.AddDays(-1);
        var service = CreateService();

        var act = () => service.GetLoginHistoryAsync(userId, 1, 10, startDate, endDate);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Start date must not be after end date.*")
            .WithParameterName("startDate");
    }

    #endregion

    #region GetRecentLoginsAsync

    [Fact]
    public async Task GetRecentLoginsAsync_ReturnsItemsFromRepository()
    {
        var userId = Guid.NewGuid();
        var count = 5;
        var loginHistory1 = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Local,
            IsSuccessful = true,
            CreatedAt = DateTime.UtcNow
        };
        var loginHistory2 = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Google,
            IsSuccessful = true,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };
        var items = new List<LoginHistory> { loginHistory1, loginHistory2 };

        _loginHistoryRepositoryMock.Setup(x => x.GetRecentByUserIdAsync(userId, count, default))
            .ReturnsAsync(items);

        var service = CreateService();
        var result = await service.GetRecentLoginsAsync(userId, count);

        result.Should().HaveCount(2);
        result.Should().Contain(new[] { loginHistory1, loginHistory2 });
        _loginHistoryRepositoryMock.Verify(x => x.GetRecentByUserIdAsync(userId, count, default), Times.Once);
    }

    #endregion

    #region GetFailedAttemptsAsync

    [Fact]
    public async Task GetFailedAttemptsAsync_ReturnsItemsFromRepositoryWithCorrectSinceDate()
    {
        var userId = Guid.NewGuid();
        var since = DateTime.UtcNow.AddHours(-1);
        var failedLogin1 = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Local,
            IsSuccessful = false,
            FailureReason = "Invalid password",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30)
        };
        var failedLogin2 = new LoginHistory
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = LoginProvider.Local,
            IsSuccessful = false,
            FailureReason = "Account locked",
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };
        var items = new List<LoginHistory> { failedLogin1, failedLogin2 };

        _loginHistoryRepositoryMock.Setup(x => x.GetFailedAttemptsAsync(userId, since, default))
            .ReturnsAsync(items);

        var service = CreateService();
        var result = await service.GetFailedAttemptsAsync(userId, since);

        result.Should().HaveCount(2);
        result.Should().Contain(new[] { failedLogin1, failedLogin2 });
        _loginHistoryRepositoryMock.Verify(x => x.GetFailedAttemptsAsync(userId, since, default), Times.Once);
    }

    #endregion

    #region CleanupOldRecordsAsync

    [Fact]
    public async Task CleanupOldRecordsAsync_RemovesRecordsOlderThanRetentionPeriodAndReturnsCount()
    {
        var retentionDays = 90;
        var options = new DainnUserOptions { LoginHistoryRetentionDays = retentionDays };
        var expectedCutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
        var removedCount = 15;

        _loginHistoryRepositoryMock.Setup(x => x.RemoveOldRecordsAsync(
                It.Is<DateTime>(d => Math.Abs((d - expectedCutoffDate).TotalSeconds) < 5), default))
            .ReturnsAsync(removedCount);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);

        var service = CreateService(options);
        var result = await service.CleanupOldRecordsAsync();

        result.Should().Be(removedCount);
        _loginHistoryRepositoryMock.Verify(x => x.RemoveOldRecordsAsync(
            It.Is<DateTime>(d => Math.Abs((d - expectedCutoffDate).TotalSeconds) < 5), default), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CleanupOldRecordsAsync_Returns0_WhenNoRecordsToClean()
    {
        var retentionDays = 90;
        var options = new DainnUserOptions { LoginHistoryRetentionDays = retentionDays };
        var removedCount = 0;

        _loginHistoryRepositoryMock.Setup(x => x.RemoveOldRecordsAsync(It.IsAny<DateTime>(), default))
            .ReturnsAsync(removedCount);

        var service = CreateService(options);
        var result = await service.CleanupOldRecordsAsync();

        result.Should().Be(0);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    #endregion
}
