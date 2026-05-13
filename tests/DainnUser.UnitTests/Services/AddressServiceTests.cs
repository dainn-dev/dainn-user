using System.Linq.Expressions;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Models.Address;
using FluentAssertions;
using Moq;

namespace DainnUser.UnitTests.Services;

public class AddressServiceTests
{
    private readonly Mock<IUserRepository> _userRepoMock = new();
    private readonly Mock<IAddressRepository> _addressRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly AddressService _service;

    public AddressServiceTests()
    {
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _service = new AddressService(_userRepoMock.Object, _addressRepoMock.Object, _unitOfWorkMock.Object);
    }

    private void SetupUserExists(Guid userId)
    {
        _userRepoMock.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), default))
            .ReturnsAsync(true);
    }

    [Fact]
    public async Task GetAddressesAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepoMock.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), default)).ReturnsAsync(false);

        var act = () => _service.GetAddressesAsync(userId);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task GetAddressesAsync_WhenNoAddresses_ReturnsEmptyList()
    {
        var userId = Guid.NewGuid();
        SetupUserExists(userId);
        _addressRepoMock.Setup(x => x.GetByUserIdAsync(userId, default)).ReturnsAsync([]);

        var result = await _service.GetAddressesAsync(userId);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAddressesAsync_ReturnsAllAddresses()
    {
        var userId = Guid.NewGuid();
        SetupUserExists(userId);
        var addresses = new List<UserAddress>
        {
            new() { Id = Guid.NewGuid(), UserId = userId, AddressLine1 = "123 Main St", City = "Hanoi", Country = "Vietnam", AddressType = "Home", IsDefault = true },
            new() { Id = Guid.NewGuid(), UserId = userId, AddressLine1 = "456 Work St", City = "HCMC", Country = "Vietnam", AddressType = "Work", IsDefault = false }
        };
        _addressRepoMock.Setup(x => x.GetByUserIdAsync(userId, default)).ReturnsAsync(addresses);

        var result = await _service.GetAddressesAsync(userId);

        result.Should().HaveCount(2);
        result[0].IsDefault.Should().BeTrue();
        result[0].AddressLine1.Should().Be("123 Main St");
    }

    [Fact]
    public async Task AddAddressAsync_FirstAddress_SetsAsDefault()
    {
        var userId = Guid.NewGuid();
        SetupUserExists(userId);
        _addressRepoMock.Setup(x => x.UserHasAddressesAsync(userId, default)).ReturnsAsync(false);

        var dto = new AddAddressDto { AddressLine1 = "123 Main St", City = "Hanoi", Country = "Vietnam" };

        var result = await _service.AddAddressAsync(userId, dto);

        result.IsDefault.Should().BeTrue();
        _addressRepoMock.Verify(x => x.AddAsync(It.IsAny<UserAddress>(), default), Times.Once);
    }

    [Fact]
    public async Task AddAddressAsync_WithSetAsDefault_ClearsExistingDefault()
    {
        var userId = Guid.NewGuid();
        SetupUserExists(userId);
        _addressRepoMock.Setup(x => x.UserHasAddressesAsync(userId, default)).ReturnsAsync(true);

        var dto = new AddAddressDto { AddressLine1 = "123 Main St", City = "Hanoi", Country = "Vietnam", SetAsDefault = true };

        var result = await _service.AddAddressAsync(userId, dto);

        result.IsDefault.Should().BeTrue();
        _addressRepoMock.Verify(x => x.ClearDefaultForUserAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task UpdateAddressAsync_NonExistent_ThrowsAddressNotFoundException()
    {
        var userId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        SetupUserExists(userId);
        _addressRepoMock.Setup(x => x.GetByUserIdAndIdAsync(userId, addressId, default)).ReturnsAsync((UserAddress?)null);

        var dto = new UpdateAddressDto { City = "New City" };

        var act = () => _service.UpdateAddressAsync(userId, addressId, dto);

        await act.Should().ThrowAsync<AddressNotFoundException>();
    }

    [Fact]
    public async Task UpdateAddressAsync_UpdatesFields()
    {
        var userId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        SetupUserExists(userId);
        var address = new UserAddress { Id = addressId, UserId = userId, AddressLine1 = "Old St", City = "Old City", Country = "Vietnam" };
        _addressRepoMock.Setup(x => x.GetByUserIdAndIdAsync(userId, addressId, default)).ReturnsAsync(address);

        var dto = new UpdateAddressDto { City = "New City", AddressLine1 = "New St" };

        var result = await _service.UpdateAddressAsync(userId, addressId, dto);

        result.City.Should().Be("New City");
        result.AddressLine1.Should().Be("New St");
    }

    [Fact]
    public async Task DeleteAddressAsync_DefaultAddress_PromotesAnother()
    {
        var userId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        SetupUserExists(userId);
        var addresses = new List<UserAddress>
        {
            new() { Id = addressId, UserId = userId, IsDefault = true, AddressLine1 = "S1", City = "C1", Country = "V" },
            new() { Id = Guid.NewGuid(), UserId = userId, IsDefault = false, AddressLine1 = "S2", City = "C2", Country = "V" }
        };
        _addressRepoMock.Setup(x => x.GetByUserIdAndIdAsync(userId, addressId, default)).ReturnsAsync(addresses[0]);
        _addressRepoMock.Setup(x => x.GetByUserIdAsync(userId, default)).ReturnsAsync([addresses[1]]);

        await _service.DeleteAddressAsync(userId, addressId);

        addresses[1].IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task SetDefaultAddressAsync_ChangesDefault()
    {
        var userId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        SetupUserExists(userId);
        var address = new UserAddress { Id = addressId, UserId = userId, IsDefault = false, AddressLine1 = "S1", City = "C1", Country = "V" };
        _addressRepoMock.Setup(x => x.GetByUserIdAndIdAsync(userId, addressId, default)).ReturnsAsync(address);

        var result = await _service.SetDefaultAddressAsync(userId, addressId);

        result.IsDefault.Should().BeTrue();
        _addressRepoMock.Verify(x => x.ClearDefaultForUserAsync(userId, default), Times.Once);
    }

    [Fact]
    public async Task SetDefaultAddressAsync_NonExistent_ThrowsAddressNotFoundException()
    {
        var userId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        SetupUserExists(userId);
        _addressRepoMock.Setup(x => x.GetByUserIdAndIdAsync(userId, addressId, default)).ReturnsAsync((UserAddress?)null);

        var act = () => _service.SetDefaultAddressAsync(userId, addressId);

        await act.Should().ThrowAsync<AddressNotFoundException>();
    }
}
