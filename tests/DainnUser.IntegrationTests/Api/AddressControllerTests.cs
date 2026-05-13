using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DainnUser.Application.DTOs.Authentication;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using ApiAuthDtos = DainnUser.Api.DTOs.Authentication;
using ApiDTOs = DainnUser.Api.DTOs;
using ApiAddress = DainnUser.Api.DTOs.Address;

namespace DainnUser.IntegrationTests.Api;

/// <summary>
/// Integration tests for AddressController endpoints.
/// </summary>
public class AddressControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AddressControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.EnsureDatabaseCreated();
    }

    [Fact]
    public async Task GetAddresses_WithoutAuth_Returns401()
    {
        var response = await _client.GetAsync("/api/profile/addresses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAddresses_WithValidAuth_ReturnsEmptyList()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr1@test.com", "addruser1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.GetAsync("/api/profile/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<IReadOnlyList<ApiAddress.AddressResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAddress_WithValidData_ReturnsCreatedAddress()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr2@test.com", "addruser2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressType = "Home",
            PostalCode = "100000"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/profile/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AddressLine1.Should().Be("123 Main St");
        result.Data.City.Should().Be("Hanoi");
        result.Data.Country.Should().Be("Vietnam");
        result.Data.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task AddAddress_WithMissingRequired_Returns400()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr3@test.com", "addruser3");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "",
            City = "",
            Country = ""
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/profile/addresses", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Errors.Should().NotBeNull();
        result.Errors.Should().Contain(e => e.Contains("required"));
    }

    [Fact]
    public async Task GetAddresses_AfterAddingAddresses_ReturnsAllAddresses()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr4@test.com", "addruser4");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var request1 = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressType = "Home"
        };
        var request2 = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "456 Work Ave",
            City = "HCMC",
            Country = "Vietnam",
            AddressType = "Work"
        };

        await _client.PostAsJsonAsync("/api/profile/addresses", request1);
        await _client.PostAsJsonAsync("/api/profile/addresses", request2);

        // Act
        var response = await _client.GetAsync("/api/profile/addresses");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<IReadOnlyList<ApiAddress.AddressResponse>>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddAddress_SetAsDefault_SetsAsDefault()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr5@test.com", "addruser5");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Add first address
        var request1 = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressType = "Home"
        };
        var firstResponse = await _client.PostAsJsonAsync("/api/profile/addresses", request1);
        var firstResult = await firstResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();

        // Act - Add second address as default
        var request2 = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "456 Work Ave",
            City = "HCMC",
            Country = "Vietnam",
            AddressType = "Work",
            SetAsDefault = true
        };
        var response = await _client.PostAsJsonAsync("/api/profile/addresses", request2);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        result!.Data!.IsDefault.Should().BeTrue();

        // Verify the first address is no longer default
        var getResponse = await _client.GetAsync("/api/profile/addresses");
        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<IReadOnlyList<ApiAddress.AddressResponse>>>();
        var defaultAddresses = getResult!.Data!.Where(a => a.IsDefault).ToList();
        defaultAddresses.Should().HaveCount(1);
        defaultAddresses[0].Id.Should().Be(result.Data.Id);
    }

    [Fact]
    public async Task UpdateAddress_WithValidData_UpdatesAddress()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr6@test.com", "addruser6");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var addRequest = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam"
        };
        var addResponse = await _client.PostAsJsonAsync("/api/profile/addresses", addRequest);
        var addResult = await addResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();
        var addressId = addResult!.Data!.Id;

        var updateRequest = new ApiAddress.UpdateAddressRequest
        {
            City = "HCMC",
            AddressLine1 = "456 New Ave"
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/profile/addresses/{addressId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data!.City.Should().Be("HCMC");
        result.Data.AddressLine1.Should().Be("456 New Ave");
    }

    [Fact]
    public async Task UpdateAddress_NonExistent_Returns404()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr7@test.com", "addruser7");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var updateRequest = new ApiAddress.UpdateAddressRequest { City = "HCMC" };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/profile/addresses/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAddress_ExistingAddress_ReturnsSuccess()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr8@test.com", "addruser8");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var addRequest = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam"
        };
        var addResponse = await _client.PostAsJsonAsync("/api/profile/addresses", addRequest);
        var addResult = await addResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();
        var addressId = addResult!.Data!.Id;

        // Act
        var response = await _client.DeleteAsync($"/api/profile/addresses/{addressId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify deleted
        var getResponse = await _client.GetAsync("/api/profile/addresses");
        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<IReadOnlyList<ApiAddress.AddressResponse>>>();
        getResult!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task SetDefaultAddress_ExistingAddress_SetsAsDefault()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr9@test.com", "addruser9");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Add two addresses
        var addRequest1 = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam"
        };
        var addRequest2 = new ApiAddress.AddAddressRequest
        {
            AddressLine1 = "456 Work Ave",
            City = "HCMC",
            Country = "Vietnam"
        };
        var response1 = await _client.PostAsJsonAsync("/api/profile/addresses", addRequest1);
        var response2 = await _client.PostAsJsonAsync("/api/profile/addresses", addRequest2);
        var result2 = await response2.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();
        var addressId2 = result2!.Data!.Id;

        // Act
        var response = await _client.PostAsync($"/api/profile/addresses/{addressId2}/default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiDTOs.ApiResponse<ApiAddress.AddressResponse>>();
        result.Should().NotBeNull();
        result!.Data!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultAddress_NonExistent_Returns404()
    {
        // Arrange
        var (accessToken, _) = await RegisterAndLoginUser("addr10@test.com", "addruser10");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act
        var response = await _client.PostAsync($"/api/profile/addresses/{Guid.NewGuid()}/default", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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