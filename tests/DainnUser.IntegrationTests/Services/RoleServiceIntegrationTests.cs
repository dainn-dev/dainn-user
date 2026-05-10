using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DainnUser.Application.Services;
using DainnUser.Core.Authorization;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using DainnUser.Infrastructure.Repositories;
using DainnUser.Infrastructure.Services;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace DainnUser.IntegrationTests.Services;

public class RoleServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private const string TestJwtSecret = "test-secret-must-be-at-least-32-bytes-long-please-okay";

    private readonly DatabaseFixture _fixture;
    private readonly RoleService _roleService;
    private readonly AuthenticationService _authenticationService;
    private readonly IPasswordHasher<User> _passwordHasher;

    public RoleServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase();

        _passwordHasher = new PasswordHasher<User>();
        var userRepository = new UserRepository(_fixture.DbContext);
        var roleRepository = new RoleRepository(_fixture.DbContext);
        var unitOfWork = new UnitOfWork(_fixture.DbContext);
        var options = new DainnUserOptions { RequireEmailVerification = false };
        IJwtTokenService jwtTokenService = new JwtTokenService(
            Options.Create(new JwtOptions { Secret = TestJwtSecret }),
            options);

        _roleService = new RoleService(roleRepository, userRepository, unitOfWork);
        _authenticationService = new AuthenticationService(
            userRepository,
            unitOfWork,
            Mock.Of<IEmailService>(),
            _passwordHasher,
            jwtTokenService,
            options);
    }

    [Fact]
    public async Task CreateRoleAsync_PersistsRoleWithNormalizedPermissions()
    {
        var roleId = await _roleService.CreateRoleAsync(
            " Admin ",
            "  Administrators  ",
            new[] { "Users:Delete", "users:read", "users:delete" });

        var role = await _fixture.DbContext.Roles.FindAsync(roleId);

        role.Should().NotBeNull();
        role!.Name.Should().Be("Admin");
        role.Description.Should().Be("Administrators");
        role.Permissions.Should().Be("users:delete,users:read");
    }

    [Fact]
    public async Task AssignAndRemoveRole_EndToEnd_UpdatesUserRoles()
    {
        var user = await CreateActiveUserAsync("rbac@example.com", "rbacuser", "Test123!@#");
        var roleId = await _roleService.CreateRoleAsync("Manager", null, new[] { "users:read" });

        var assigned = await _roleService.AssignRoleToUserAsync(user.Id, roleId);
        var rolesAfterAssign = await _roleService.GetUserRolesAsync(user.Id);

        assigned.Should().BeTrue();
        rolesAfterAssign.Should().ContainSingle(r => r.Id == roleId);
        _fixture.DbContext.UserRoles.Should().ContainSingle(ur => ur.UserId == user.Id && ur.RoleId == roleId);

        var removed = await _roleService.RemoveRoleFromUserAsync(user.Id, roleId);
        var rolesAfterRemove = await _roleService.GetUserRolesAsync(user.Id);

        removed.Should().BeTrue();
        rolesAfterRemove.Should().BeEmpty();
        _fixture.DbContext.UserRoles.Should().NotContain(ur => ur.UserId == user.Id && ur.RoleId == roleId);
    }

    [Fact]
    public async Task LoginAsync_WithAssignedRole_EmitsRoleAndPermissionClaims()
    {
        var user = await CreateActiveUserAsync("claims@example.com", "claimsuser", "Test123!@#");
        var roleId = await _roleService.CreateRoleAsync("Admin", null, new[] { DainnUserPermissions.UsersDelete });
        await _roleService.AssignRoleToUserAsync(user.Id, roleId);
        _fixture.DbContext.ChangeTracker.Clear();

        var result = await _authenticationService.LoginAsync(user.Email, "Test123!@#", "127.0.0.1", "test-agent");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        jwt.Claims.Should().Contain(c => c.Type == DainnUserClaimTypes.Permission && c.Value == DainnUserPermissions.UsersDelete);
    }

    [Fact]
    public async Task DeleteRoleAsync_WhenAssigned_ThrowsAndPreservesRole()
    {
        var user = await CreateActiveUserAsync("delete@example.com", "deleteuser", "Test123!@#");
        var roleId = await _roleService.CreateRoleAsync("Protected", null, Array.Empty<string>());
        await _roleService.AssignRoleToUserAsync(user.Id, roleId);

        var act = () => _roleService.DeleteRoleAsync(roleId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot delete a role assigned to users.");
        (await _fixture.DbContext.Roles.FindAsync(roleId)).Should().NotBeNull();
    }

    private async Task<User> CreateActiveUserAsync(string email, string username, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Username = username,
            Status = UserStatus.Active,
            EmailVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        _fixture.DbContext.Users.Add(user);
        await _fixture.DbContext.SaveChangesAsync();
        return user;
    }
}
