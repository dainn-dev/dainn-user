using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DainnUser.Api.Controllers;
using DainnUser.Api.DTOs;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Data;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ApiAuthDtos = DainnUser.Api.DTOs.Authentication;

namespace DainnUser.IntegrationTests.Api;

/// <summary>
/// Integration tests for UserController endpoints (admin user management).
/// </summary>
public class UserControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task GetUsers_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_WithNonAdminUser_Returns403()
    {
        var (accessToken, _) = await RegisterAndLoginUser("user@test.com", "regularuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync("/api/user");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_WithAdminUser_ReturnsPagedUsers()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin1@test.com", "admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await RegisterAndLoginUser("testuser1@test.com", "testuser1");
        await RegisterAndLoginUser("testuser2@test.com", "testuser2");

        var response = await _client.GetAsync("/api/user?pageNumber=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().NotBeEmpty();
        result.Data.TotalCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetUsers_WithSearch_ReturnsFilteredUsers()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin2@test.com", "admin2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await RegisterAndLoginUser("searchme@test.com", "searchme");

        var response = await _client.GetAsync("/api/user?search=searchme");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<UserDto>>>();
        result!.Data!.Items.Should().Contain(u => u.Username == "searchme");
    }

    [Fact]
    public async Task GetUser_WithValidId_ReturnsUser()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin3@test.com", "admin3");
        var (_, targetUserId) = await RegisterAndLoginUser("target@test.com", "targetuser");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync($"/api/user/{targetUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(targetUserId);
        result.Data.Username.Should().Be("targetuser");
    }

    [Fact]
    public async Task GetUser_WithInvalidId_Returns404()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin4@test.com", "admin4");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.GetAsync($"/api/user/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUser_WithValidData_UpdatesUser()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin5@test.com", "admin5");
        var (_, targetUserId) = await RegisterAndLoginUser("updateme@test.com", "updateme");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateDto = new UpdateUserDto
        {
            Email = "updated@test.com",
            Username = "updateduser",
            Status = UserStatus.Suspended
        };

        var response = await _client.PutAsJsonAsync($"/api/user/{targetUserId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result!.Data!.Email.Should().Be("updated@test.com");
        result.Data.Username.Should().Be("updateduser");
        result.Data.Status.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public async Task UpdateUser_WithTakenEmail_Returns400()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin6@test.com", "admin6");
        await RegisterAndLoginUser("existing@test.com", "existing");
        var (_, targetUserId) = await RegisterAndLoginUser("target2@test.com", "target2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateDto = new UpdateUserDto { Email = "existing@test.com" };

        var response = await _client.PutAsJsonAsync($"/api/user/{targetUserId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteUser_WithValidId_DeletesUser()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin7@test.com", "admin7");
        var (_, targetUserId) = await RegisterAndLoginUser("deleteme@test.com", "deleteme");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.DeleteAsync($"/api/user/{targetUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/user/{targetUserId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteUser_OwnAccount_Returns400()
    {
        var (accessToken, adminUserId) = await RegisterAndLoginAdmin("admin8@test.com", "admin8");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.DeleteAsync($"/api/user/{adminUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Message.Should().Contain("Cannot delete your own account");
    }

    [Fact]
    public async Task LockUser_WithValidId_LocksUser()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin9@test.com", "admin9");
        var (_, targetUserId) = await RegisterAndLoginUser("lockme@test.com", "lockme");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsync($"/api/user/{targetUserId}/lock", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/user/{targetUserId}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result!.Data!.Status.Should().Be(UserStatus.Locked);
    }

    [Fact]
    public async Task LockUser_OwnAccount_Returns400()
    {
        var (accessToken, adminUserId) = await RegisterAndLoginAdmin("admin10@test.com", "admin10");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsync($"/api/user/{adminUserId}/lock", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<object>>();
        result!.Message.Should().Contain("Cannot lock your own account");
    }

    [Fact]
    public async Task UnlockUser_WithLockedUser_UnlocksUser()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin11@test.com", "admin11");
        var (_, targetUserId) = await RegisterAndLoginUser("unlockme@test.com", "unlockme");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        await _client.PostAsync($"/api/user/{targetUserId}/lock", null);

        var response = await _client.PostAsync($"/api/user/{targetUserId}/unlock", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await _client.GetAsync($"/api/user/{targetUserId}");
        var result = await getResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        result!.Data!.Status.Should().Be(UserStatus.Active);
    }

    [Fact]
    public async Task AddRoleToUser_WithValidData_AddsRole()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin12@test.com", "admin12");
        var (_, targetUserId) = await RegisterAndLoginUser("roletest@test.com", "roletest");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var roleId = await GetRoleIdByName("User");
        var request = new { RoleId = roleId };

        var response = await _client.PostAsJsonAsync($"/api/user/{targetUserId}/roles", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveRoleFromUser_WithValidData_RemovesRole()
    {
        var (accessToken, _) = await RegisterAndLoginAdmin("admin13@test.com", "admin13");
        var (_, targetUserId) = await RegisterAndLoginUser("removerole@test.com", "removerole");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var roleId = await GetRoleIdByName("User");
        await _client.PostAsJsonAsync($"/api/user/{targetUserId}/roles", new { RoleId = roleId });

        var response = await _client.DeleteAsync($"/api/user/{targetUserId}/roles/{roleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginUser(string email, string username)
    {
        var registerDto = new RegisterDto
        {
            Email = email,
            Username = username,
            Password = "Test123!@#",
            ConfirmPassword = "Test123!@#"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerDto);
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<ApiAuthDtos.RegisterResponse>>();

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DainnUserDbContext>();
        var user = dbContext.Users.First(u => u.Email == email);
        user.EmailVerified = true;
        await dbContext.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = email,
            Password = "Test123!@#"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<ApiAuthDtos.LoginResponse>>();

        return (loginResult!.Data!.AccessToken, user.Id);
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginAdmin(string email, string username)
    {
        var (accessToken, userId) = await RegisterAndLoginUser(email, username);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DainnUserDbContext>();
        var adminRole = dbContext.Roles.First(r => r.Name == "Administrator");
        var userRole = new UserRole { UserId = userId, RoleId = adminRole.Id };
        dbContext.UserRoles.Add(userRole);
        await dbContext.SaveChangesAsync();

        var loginDto = new LoginDto
        {
            Email = email,
            Password = "Test123!@#"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginDto);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<ApiAuthDtos.LoginResponse>>();

        return (loginResult!.Data!.AccessToken, userId);
    }

    private async Task<Guid> GetRoleIdByName(string roleName)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DainnUserDbContext>();
        var role = dbContext.Roles.First(r => r.Name == roleName);
        return role.Id;
    }
}
