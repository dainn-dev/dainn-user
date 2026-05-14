using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ApiAuthDtos = DainnUser.Api.DTOs.Authentication;
using ApiContact = DainnUser.Api.DTOs.Contact;
using ApiDTOs = DainnUser.Api.DTOs;

namespace DainnUser.IntegrationTests.Api;

/// <summary>
/// Integration tests for ContactController endpoints.
/// </summary>
public class ContactControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ContactControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task GetContacts_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/profile/contacts");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddContact_WithValidEmail_ReturnsCreated()
    {
        var (accessToken, _) = await RegisterAndLoginUser("contact1@test.com", "contactuser1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/profile/contacts", new ApiContact.AddContactRequest
        {
            ContactType = "Email",
            ContactValue = "alternate@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiContact.ContactResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.ContactType.Should().Be("Email");
        result.Data.ContactValue.Should().Be("alternate@example.com");
        result.Data.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task AddContact_WithInvalidPhone_ReturnsBadRequest()
    {
        var (accessToken, _) = await RegisterAndLoginUser("contact2@test.com", "contactuser2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _client.PostAsJsonAsync("/api/profile/contacts", new ApiContact.AddContactRequest
        {
            ContactType = "Phone",
            ContactValue = "0901234567"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetContact_WhenOwnedByAnotherUser_Returns404()
    {
        var (ownerToken, _) = await RegisterAndLoginUser("contact3@test.com", "contactuser3");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
        var addResponse = await _client.PostAsJsonAsync("/api/profile/contacts", new ApiContact.AddContactRequest
        {
            ContactType = "Email",
            ContactValue = "private@example.com"
        });
        var addResult = await addResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiContact.ContactResponse>>();

        var (otherToken, _) = await RegisterAndLoginUser("contact4@test.com", "contactuser4");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", otherToken);

        var response = await _client.GetAsync($"/api/profile/contacts/{addResult!.Data!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateContact_WhenValueChanges_ReturnsUnverifiedContact()
    {
        var (accessToken, _) = await RegisterAndLoginUser("contact5@test.com", "contactuser5");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var addResponse = await _client.PostAsJsonAsync("/api/profile/contacts", new ApiContact.AddContactRequest
        {
            ContactType = "Email",
            ContactValue = "old@example.com"
        });
        var addResult = await addResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiContact.ContactResponse>>();

        var response = await _client.PutAsJsonAsync($"/api/profile/contacts/{addResult!.Data!.Id}", new ApiContact.UpdateContactRequest
        {
            ContactValue = "new@example.com"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiContact.ContactResponse>>();
        result!.Data!.ContactValue.Should().Be("new@example.com");
        result.Data.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task SetPrimaryContact_ClearsPreviousPrimaryForSameType()
    {
        var (accessToken, _) = await RegisterAndLoginUser("contact6@test.com", "contactuser6");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        await _client.PostAsJsonAsync("/api/profile/contacts", new ApiContact.AddContactRequest
        {
            ContactType = "Email",
            ContactValue = "first@example.com"
        });
        var secondResponse = await _client.PostAsJsonAsync("/api/profile/contacts", new ApiContact.AddContactRequest
        {
            ContactType = "Email",
            ContactValue = "second@example.com"
        });
        var secondResult = await secondResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiContact.ContactResponse>>();

        var response = await _client.PostAsync($"/api/profile/contacts/{secondResult!.Data!.Id}/primary", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResponse = await _client.GetAsync("/api/profile/contacts");
        var contacts = await getResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<IReadOnlyList<ApiContact.ContactResponse>>>();
        contacts!.Data!.Where(c => c.IsPrimary && c.ContactType == "Email").Should().ContainSingle()
            .Which.ContactValue.Should().Be("second@example.com");
    }

    [Fact]
    public async Task DeleteContact_RemovesContact()
    {
        var (accessToken, _) = await RegisterAndLoginUser("contact7@test.com", "contactuser7");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var addResponse = await _client.PostAsJsonAsync("/api/profile/contacts", new ApiContact.AddContactRequest
        {
            ContactType = "Email",
            ContactValue = "delete@example.com"
        });
        var addResult = await addResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiContact.ContactResponse>>();

        var response = await _client.DeleteAsync($"/api/profile/contacts/{addResult!.Data!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResponse = await _client.GetAsync("/api/profile/contacts");
        var contacts = await getResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<IReadOnlyList<ApiContact.ContactResponse>>>();
        contacts!.Data.Should().BeEmpty();
    }

    private async Task<(string AccessToken, Guid UserId)> RegisterAndLoginUser(string email, string username)
    {
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

        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<Infrastructure.Data.DainnUserDbContext>();
            var user = await dbContext.Users.FindAsync(userId);
            user!.EmailVerified = true;
            await dbContext.SaveChangesAsync();
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = "SecurePass123!"
        });
        loginResponse.EnsureSuccessStatusCode();
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAuthDtos.LoginResponse>>();

        return (loginResult!.Data!.AccessToken, userId);
    }
}
