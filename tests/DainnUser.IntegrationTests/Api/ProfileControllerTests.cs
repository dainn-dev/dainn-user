using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.Core.Models.Profile;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ApiAuthDtos = DainnUser.Api.DTOs.Authentication;
using ApiDTOs = DainnUser.Api.DTOs;
using ApiProfile = DainnUser.Api.DTOs.Profile;

namespace DainnUser.IntegrationTests.Api;

/// <summary>
/// Integration tests for ProfileController endpoints.
/// </summary>
public class ProfileControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ProfileControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task GetProfile_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithValidAuth_ReturnsProfile()
    {
        // Arrange: Register and login
        var (accessToken, userId) = await RegisterAndLoginUser("profile1@test.com", "profileuser1");

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.GetAsync("/api/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiProfile.ProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.UserId.Should().Be(userId);
        result.Data.Email.Should().Be("profile1@test.com");
        result.Data.Username.Should().Be("profileuser1");
    }

    [Fact]
    public async Task UpdateProfile_WithValidData_UpdatesProfile()
    {
        // Arrange
        var (accessToken, userId) = await RegisterAndLoginUser("profile2@test.com", "profileuser2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateRequest = new UpdateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Bio = "Software developer",
            Language = "en",
            Timezone = "UTC"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiProfile.ProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.FirstName.Should().Be("John");
        result.Data.LastName.Should().Be("Doe");
        result.Data.DisplayName.Should().Be("John Doe");
        result.Data.Bio.Should().Be("Software developer");
        result.Data.Language.Should().Be("en");
        result.Data.Timezone.Should().Be("UTC");
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidTimezone_Returns400()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("profile3@test.com", "profileuser3");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateRequest = new UpdateProfileDto
        {
            Timezone = "Invalid/Timezone"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiProfile.ProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().NotBeNull();
        result.Errors.Should().Contain(e => e.Contains("Timezone"));
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidLanguage_Returns400()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("profile4@test.com", "profileuser4");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateRequest = new UpdateProfileDto
        {
            Language = "invalid"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiProfile.ProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Language"));
    }

    [Fact]
    public async Task UpdateProfile_WithInvalidWebsite_Returns400()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("profile5@test.com", "profileuser5");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateRequest = new UpdateProfileDto
        {
            Website = "not-a-url"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/profile", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiProfile.ProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Website"));
    }

    [Fact]
    public async Task UpdateSettings_UpdatesLanguageAndTimezone()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("profile6@test.com", "profileuser6");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateRequest = new UpdateProfileDto
        {
            Language = "vi",
            Timezone = "Asia/Ho_Chi_Minh"
        };

        // Act
        var response = await _client.PutAsJsonAsync("/api/profile/settings", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiProfile.ProfileResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.Language.Should().Be("vi");
        result.Data.Timezone.Should().Be("Asia/Ho_Chi_Minh");
    }

    [Fact]
    public async Task UploadAvatar_WithInvalidImage_Returns400()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("profile7@test.com", "profileuser7");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(fileContent, "file", "avatar.png");

        // Act
        var response = await _client.PostAsync("/api/profile/avatar", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteAvatar_WithNoAvatar_ReturnsOk()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("profile8@test.com", "profileuser8");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.DeleteAsync("/api/profile/avatar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginUser(string email, string username)
    {
        // Register
        var registerRequest = new RegisterDto
        {
            Email = email,
            Username = username,
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!"
        };

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerResult = await registerResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAuthDtos.RegisterResponse>>();
        var userId = registerResult!.Data!.UserId;

        // Verify email (simulate)
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.DainnUserDbContext>();
            var user = await dbContext.Users.FindAsync(userId);
            user!.EmailVerified = true;
            await dbContext.SaveChangesAsync();
        }

        // Login
        var loginRequest = new LoginDto
        {
            Email = email,
            Password = "SecurePass123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAuthDtos.LoginResponse>>();

        return (loginResult!.Data!.AccessToken, userId);
    }
}
