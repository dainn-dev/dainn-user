using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using FluentAssertions;
using Moq;

namespace DainnUser.UnitTests.Services;

public class UserManagementServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRoleRepository> _roleRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IUserManagementService _service;

    public UserManagementServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _roleRepositoryMock = new Mock<IRoleRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _service = new UserManagementService(
            _userRepositoryMock.Object,
            _roleRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task GetUsersAsync_WithNoFilters_ReturnsPagedUsers()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "user1@test.com", Username = "user1", Status = UserStatus.Active },
            new() { Id = Guid.NewGuid(), Email = "user2@test.com", Username = "user2", Status = UserStatus.Active }
        };

        _userRepositoryMock
            .Setup(x => x.GetPagedAsync(1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 2));

        _roleRepositoryMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        var result = await _service.GetUsersAsync(1, 20);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetUsersAsync_WithSearch_ReturnsFilteredUsers()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "john@test.com", Username = "john", Status = UserStatus.Active }
        };

        _userRepositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 1));

        _roleRepositoryMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        var result = await _service.GetUsersAsync(1, 20, "john");

        result.Items.Should().HaveCount(1);
        result.Items.First().Username.Should().Be("john");
    }

    [Fact]
    public async Task GetUsersAsync_WithStatus_ReturnsFilteredUsers()
    {
        var users = new List<User>
        {
            new() { Id = Guid.NewGuid(), Email = "locked@test.com", Username = "locked", Status = UserStatus.Locked }
        };

        _userRepositoryMock
            .Setup(x => x.GetPagedAsync(It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users, 1));

        _roleRepositoryMock
            .Setup(x => x.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        var result = await _service.GetUsersAsync(1, 20, null, UserStatus.Locked);

        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be(UserStatus.Locked);
    }

    [Fact]
    public async Task GetUsersAsync_ClampsPageSize()
    {
        _userRepositoryMock
            .Setup(x => x.GetPagedAsync(1, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<User>(), 0));

        await _service.GetUsersAsync(1, 200);

        _userRepositoryMock.Verify(x => x.GetPagedAsync(1, 100, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetUserByIdAsync_UserExists_ReturnsUserDto()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com", Username = "test", Status = UserStatus.Active };
        var roles = new List<Role> { new() { Id = Guid.NewGuid(), Name = "User" } };

        _userRepositoryMock
            .Setup(x => x.GetWithRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _roleRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var result = await _service.GetUserByIdAsync(userId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
        result.Roles.Should().Contain("User");
    }

    [Fact]
    public async Task GetUserByIdAsync_UserNotFound_ReturnsNull()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetWithRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await _service.GetUserByIdAsync(userId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUserAsync_ValidData_UpdatesUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "old@test.com", Username = "olduser", Status = UserStatus.Active };
        var dto = new UpdateUserDto { Email = "new@test.com", Username = "newuser", Status = UserStatus.Suspended };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.IsEmailTakenAsync("new@test.com", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _userRepositoryMock
            .Setup(x => x.IsUsernameTakenAsync("newuser", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _roleRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Role>());

        var result = await _service.UpdateUserAsync(userId, dto);

        result.Email.Should().Be("new@test.com");
        result.Username.Should().Be("newuser");
        result.Status.Should().Be(UserStatus.Suspended);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUserAsync_UserNotFound_ThrowsException()
    {
        var userId = Guid.NewGuid();
        var dto = new UpdateUserDto { Email = "new@test.com" };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await _service.Invoking(s => s.UpdateUserAsync(userId, dto))
            .Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task UpdateUserAsync_EmailTaken_ThrowsException()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "old@test.com", Username = "user", Status = UserStatus.Active };
        var dto = new UpdateUserDto { Email = "taken@test.com" };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(x => x.IsEmailTakenAsync("taken@test.com", userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.Invoking(s => s.UpdateUserAsync(userId, dto))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already taken*");
    }

    [Fact]
    public async Task DeleteUserAsync_UserExists_DeletesUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com", Username = "test", Status = UserStatus.Active };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.DeleteUserAsync(userId);

        _userRepositoryMock.Verify(x => x.Remove(user), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ThrowsException()
    {
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        await _service.Invoking(s => s.DeleteUserAsync(userId))
            .Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task LockUserAsync_UserExists_LocksUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com", Username = "test", Status = UserStatus.Active };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.LockUserAsync(userId);

        user.Status.Should().Be(UserStatus.Locked);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnlockUserAsync_UserExists_UnlocksUser()
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "test@test.com",
            Username = "test",
            Status = UserStatus.Locked,
            FailedLoginAttempts = 5,
            LockoutEnd = DateTime.UtcNow.AddHours(1)
        };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _service.UnlockUserAsync(userId);

        user.Status.Should().Be(UserStatus.Active);
        user.FailedLoginAttempts.Should().Be(0);
        user.LockoutEnd.Should().BeNull();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRoleToUserAsync_ValidData_AddsRole()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com", Username = "test", Status = UserStatus.Active };
        var role = new Role { Id = roleId, Name = "Admin" };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _roleRepositoryMock
            .Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(x => x.UserHasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await _service.AddRoleToUserAsync(userId, roleId);

        _roleRepositoryMock.Verify(x => x.AddUserRoleAsync(It.Is<UserRole>(ur => ur.UserId == userId && ur.RoleId == roleId), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRoleToUserAsync_UserAlreadyHasRole_DoesNothing()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@test.com", Username = "test", Status = UserStatus.Active };
        var role = new Role { Id = roleId, Name = "Admin" };

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _roleRepositoryMock
            .Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _roleRepositoryMock
            .Setup(x => x.UserHasRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await _service.AddRoleToUserAsync(userId, roleId);

        _roleRepositoryMock.Verify(x => x.AddUserRoleAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_ValidData_RemovesRole()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var userRole = new UserRole { UserId = userId, RoleId = roleId };

        _roleRepositoryMock
            .Setup(x => x.GetUserRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRole);

        await _service.RemoveRoleFromUserAsync(userId, roleId);

        _roleRepositoryMock.Verify(x => x.RemoveUserRole(userRole), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_UserDoesNotHaveRole_ThrowsException()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();

        _roleRepositoryMock
            .Setup(x => x.GetUserRoleAsync(userId, roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole?)null);

        await _service.Invoking(s => s.RemoveRoleFromUserAsync(userId, roleId))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*does not have*");
    }
}
