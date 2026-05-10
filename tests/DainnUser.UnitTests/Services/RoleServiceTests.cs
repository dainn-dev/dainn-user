using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using FluentAssertions;
using Moq;

namespace DainnUser.UnitTests.Services;

public class RoleServiceTests
{
    private readonly Mock<IRoleRepository> _roleRepositoryMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _service = new RoleService(_roleRepositoryMock.Object, _userRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateRoleAsync_WithValidRole_CreatesNormalizedRole()
    {
        Role? addedRole = null;
        _roleRepositoryMock.Setup(x => x.IsNameTakenAsync("Manager", null, default)).ReturnsAsync(false);
        _roleRepositoryMock.Setup(x => x.AddAsync(It.IsAny<Role>(), default))
            .Callback<Role, CancellationToken>((role, _) => addedRole = role)
            .Returns(Task.CompletedTask);

        var roleId = await _service.CreateRoleAsync(
            " Manager ",
            "  Team managers  ",
            new[] { "Users:Read", "users:read", " users:write " });

        roleId.Should().NotBeEmpty();
        addedRole.Should().NotBeNull();
        addedRole!.Name.Should().Be("Manager");
        addedRole.Description.Should().Be("Team managers");
        addedRole.Permissions.Should().Be("users:read,users:write");
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task CreateRoleAsync_WithDuplicateName_Throws()
    {
        _roleRepositoryMock.Setup(x => x.IsNameTakenAsync("Manager", null, default)).ReturnsAsync(true);

        var act = () => _service.CreateRoleAsync("Manager", null, Array.Empty<string>());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Role name is already taken.");
    }

    [Fact]
    public async Task UpdateRoleAsync_WithExistingRole_UpdatesFields()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Old", Permissions = "old:permission" };
        _roleRepositoryMock.Setup(x => x.GetByIdAsync(role.Id, default)).ReturnsAsync(role);
        _roleRepositoryMock.Setup(x => x.IsNameTakenAsync("New", role.Id, default)).ReturnsAsync(false);

        var updated = await _service.UpdateRoleAsync(role.Id, "New", null, new[] { "roles:write" });

        updated.Should().BeTrue();
        role.Name.Should().Be("New");
        role.Description.Should().BeNull();
        role.Permissions.Should().Be("roles:write");
        _roleRepositoryMock.Verify(x => x.Update(role), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenRoleHasUsers_Throws()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Assigned" };
        role.UserRoles.Add(new UserRole { UserId = Guid.NewGuid(), RoleId = role.Id });
        _roleRepositoryMock.Setup(x => x.GetWithUsersAsync(role.Id, default)).ReturnsAsync(role);

        var act = () => _service.DeleteRoleAsync(role.Id);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete a role assigned to users.");
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenRoleUnassigned_RemovesRole()
    {
        var role = new Role { Id = Guid.NewGuid(), Name = "Unassigned" };
        _roleRepositoryMock.Setup(x => x.GetWithUsersAsync(role.Id, default)).ReturnsAsync(role);

        var deleted = await _service.DeleteRoleAsync(role.Id);

        deleted.Should().BeTrue();
        _roleRepositoryMock.Verify(x => x.Remove(role), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AssignRoleToUserAsync_WithExistingUserAndRole_AddsAssignment()
    {
        var user = new User { Id = Guid.NewGuid() };
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        UserRole? assignment = null;
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        _roleRepositoryMock.Setup(x => x.GetByIdAsync(role.Id, default)).ReturnsAsync(role);
        _roleRepositoryMock.Setup(x => x.UserHasRoleAsync(user.Id, role.Id, default)).ReturnsAsync(false);
        _roleRepositoryMock.Setup(x => x.AddUserRoleAsync(It.IsAny<UserRole>(), default))
            .Callback<UserRole, CancellationToken>((userRole, _) => assignment = userRole)
            .Returns(Task.CompletedTask);

        var assigned = await _service.AssignRoleToUserAsync(user.Id, role.Id);

        assigned.Should().BeTrue();
        assignment.Should().NotBeNull();
        assignment!.UserId.Should().Be(user.Id);
        assignment.RoleId.Should().Be(role.Id);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task AssignRoleToUserAsync_WhenAlreadyAssigned_IsIdempotent()
    {
        var user = new User { Id = Guid.NewGuid() };
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        _userRepositoryMock.Setup(x => x.GetByIdAsync(user.Id, default)).ReturnsAsync(user);
        _roleRepositoryMock.Setup(x => x.GetByIdAsync(role.Id, default)).ReturnsAsync(role);
        _roleRepositoryMock.Setup(x => x.UserHasRoleAsync(user.Id, role.Id, default)).ReturnsAsync(true);

        var assigned = await _service.AssignRoleToUserAsync(user.Id, role.Id);

        assigned.Should().BeTrue();
        _roleRepositoryMock.Verify(x => x.AddUserRoleAsync(It.IsAny<UserRole>(), default), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task RemoveRoleFromUserAsync_WithExistingAssignment_RemovesAssignment()
    {
        var assignment = new UserRole { UserId = Guid.NewGuid(), RoleId = Guid.NewGuid() };
        _roleRepositoryMock.Setup(x => x.GetUserRoleAsync(assignment.UserId, assignment.RoleId, default)).ReturnsAsync(assignment);

        var removed = await _service.RemoveRoleFromUserAsync(assignment.UserId, assignment.RoleId);

        removed.Should().BeTrue();
        _roleRepositoryMock.Verify(x => x.RemoveUserRole(assignment), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task GetRolePermissionsAsync_ParsesStoredPermissions()
    {
        var role = new Role { Id = Guid.NewGuid(), Permissions = " Users:Read, users:write, users:read " };
        _roleRepositoryMock.Setup(x => x.GetByIdAsync(role.Id, default)).ReturnsAsync(role);

        var permissions = await _service.GetRolePermissionsAsync(role.Id);

        permissions.Should().Equal("users:read", "users:write");
    }
}
