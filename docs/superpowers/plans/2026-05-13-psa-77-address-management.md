# PSA-77: Address Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement address management for DainnUser with address CRUD, default management, validation, and tests.

**Architecture:** Addresses are sub-resources under `api/profile/addresses`. Follows existing library patterns: repository interface + implementation, service interface + implementation, FluentValidation validators, controller with `[Authorize]`, unit and integration tests.

**Tech Stack:** .NET 8, Entity Framework Core, FluentValidation, xUnit, Moq

---

## File Structure

```
DainnUser.Core/
├── Exceptions/AddressNotFoundException.cs     (NEW)
├── Interfaces/Repositories/IAddressRepository.cs  (NEW)
├── Models/Address/
│   ├── AddressDto.cs                          (NEW)
│   ├── AddAddressDto.cs                       (NEW)
│   └── UpdateAddressDto.cs                    (NEW)

DainnUser.Infrastructure/
├── Repositories/AddressRepository.cs          (NEW)
├── Data/RepositoryServiceExtensions.cs        (MODIFY: add Addresses to IUnitOfWork)
└── Data/IUnitOfWork.cs                        (MODIFY: add Addresses property)

DainnUser.Application/
├── Services/AddressService.cs                 (NEW)
├── Validators/AddAddressDtoValidator.cs       (NEW)
└── Validators/UpdateAddressDtoValidator.cs    (NEW)
└── ApplicationServiceExtensions.cs            (MODIFY: register IAddressService)

DainnUser.Api/
├── DTOs/Address/
│   ├── AddressResponse.cs                     (NEW)
│   ├── AddAddressRequest.cs                   (NEW)
│   └── UpdateAddressRequest.cs                (NEW)
└── Controllers/AddressController.cs           (NEW)

tests/
├── DainnUser.UnitTests/Services/AddressServiceTests.cs        (NEW)
├── DainnUser.UnitTests/Validators/AddAddressDtoValidatorTests.cs  (NEW)
├── DainnUser.UnitTests/Validators/UpdateAddressDtoValidatorTests.cs  (NEW)
├── DainnUser.IntegrationTests/Services/AddressServiceIntegrationTests.cs  (NEW)
└── DainnUser.IntegrationTests/Api/AddressControllerTests.cs  (NEW)
```

---

## Task 1: Create AddressNotFoundException

**Files:**
- Create: `src/DainnUser.Core/Exceptions/AddressNotFoundException.cs`

- [ ] **Step 1: Write the exception class**

```csharp
namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when an address cannot be found by the provided identifier.
/// </summary>
public class AddressNotFoundException : Exception
{
    /// <summary>
    /// Gets the address identifier that could not be found.
    /// </summary>
    public Guid AddressId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressNotFoundException"/> class.
    /// </summary>
    /// <param name="addressId">The address identifier that could not be found.</param>
    public AddressNotFoundException(Guid addressId)
        : base($"Address with id '{addressId}' was not found.")
    {
        AddressId = addressId;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/DainnUser.Core/Exceptions/AddressNotFoundException.cs
git commit -m "feat: add AddressNotFoundException"
```

---

## Task 2: Create Address DTOs in Core

**Files:**
- Create: `src/DainnUser.Core/Models/Address/AddressDto.cs`
- Create: `src/DainnUser.Core/Models/Address/AddAddressDto.cs`
- Create: `src/DainnUser.Core/Models/Address/UpdateAddressDto.cs`

- [ ] **Step 1: Write AddressDto.cs**

```csharp
namespace DainnUser.Core.Models.Address;

/// <summary>
/// Data transfer object representing an address.
/// </summary>
public class AddressDto
{
    public Guid Id { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

- [ ] **Step 2: Write AddAddressDto.cs**

```csharp
namespace DainnUser.Core.Models.Address;

/// <summary>
/// Data transfer object for creating a new address.
/// </summary>
public class AddAddressDto
{
    public string AddressType { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool SetAsDefault { get; set; }
}
```

- [ ] **Step 3: Write UpdateAddressDto.cs**

```csharp
namespace DainnUser.Core.Models.Address;

/// <summary>
/// Data transfer object for updating an existing address.
/// </summary>
public class UpdateAddressDto
{
    public string? AddressType { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
```

- [ ] **Step 4: Commit**

```bash
git add src/DainnUser.Core/Models/Address/AddressDto.cs src/DainnUser.Core/Models/Address/AddAddressDto.cs src/DainnUser.Core/Models/Address/UpdateAddressDto.cs
git commit -m "feat: add address DTOs"
```

---

## Task 3: Create IAddressRepository

**Files:**
- Create: `src/DainnUser.Core/Interfaces/Repositories/IAddressRepository.cs`

- [ ] **Step 1: Write IAddressRepository.cs**

```csharp
using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for UserAddress entity.
/// </summary>
public interface IAddressRepository : IRepository<UserAddress>
{
    /// <summary>
    /// Gets all addresses for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of user's addresses.</returns>
    Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="addressId">The address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The address if found and belongs to user, otherwise null.</returns>
    Task<UserAddress?> GetByUserIdAndIdAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the default flag for all addresses of a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the default address for a user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The default address if found, otherwise null.</returns>
    Task<UserAddress?> GetDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has any addresses.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if user has addresses, otherwise false.</returns>
    Task<bool> UserHasAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 2: Commit**

```bash
git add src/DainnUser.Core/Interfaces/Repositories/IAddressRepository.cs
git commit -m "feat: add IAddressRepository interface"
```

---

## Task 4: Create AddressRepository implementation

**Files:**
- Create: `src/DainnUser.Infrastructure/Repositories/AddressRepository.cs`

- [ ] **Step 1: Write AddressRepository.cs**

```csharp
using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DainnUser.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for UserAddress entity.
/// </summary>
public class AddressRepository : Repository<UserAddress>, IAddressRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AddressRepository(DainnUserDbContext context) : base(context)
    {
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<UserAddress?> GetByUserIdAndIdAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.UserId == userId && a.Id == addressId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task ClearDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var defaultAddresses = await _dbSet
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var address in defaultAddresses)
        {
            address.IsDefault = false;
            address.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <inheritdoc/>
    public async Task<UserAddress?> GetDefaultForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> UserHasAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(a => a.UserId == userId, cancellationToken);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/DainnUser.Infrastructure/Repositories/AddressRepository.cs
git commit -m "feat: add AddressRepository implementation"
```

---

## Task 5: Update IUnitOfWork and RepositoryServiceExtensions

**Files:**
- Modify: `src/DainnUser.Core/Interfaces/Repositories/IUnitOfWork.cs`
- Modify: `src/DainnUser.Infrastructure/Data/RepositoryServiceExtensions.cs`
- Modify: `src/DainnUser.Infrastructure/Repositories/UnitOfWork.cs`

- [ ] **Step 1: Add Addresses property to IUnitOfWork.cs**

Add after line 31 (after `IActivityLogRepository ActivityLogs`):
```csharp
    /// <summary>
    /// Gets the address repository.
    /// </summary>
    IAddressRepository Addresses { get; }
```

- [ ] **Step 2: Add Addresses property and field to UnitOfWork.cs**

Add field after line 19:
```csharp
    private IAddressRepository? _addresses;
```

Add property after line 43 (after `ActivityLogs` property):
```csharp
    /// <inheritdoc/>
    public IAddressRepository Addresses => _addresses ??= new AddressRepository(_context);
```

- [ ] **Step 3: Add Addresses to RepositoryServiceExtensions.cs**

Add after line 24 (after `services.AddScoped<IActivityLogRepository, ActivityLogRepository>();`):
```csharp
        services.AddScoped<IAddressRepository, AddressRepository>();
```

- [ ] **Step 4: Commit**

```bash
git add src/DainnUser.Core/Interfaces/Repositories/IUnitOfWork.cs src/DainnUser.Infrastructure/Data/RepositoryServiceExtensions.cs src/DainnUser.Infrastructure/Repositories/UnitOfWork.cs
git commit -m "feat: add IAddressRepository to IUnitOfWork"
```

---

## Task 6: Create AddressService

**Files:**
- Create: `src/DainnUser.Application/Services/AddressService.cs`

- [ ] **Step 1: Write AddressService.cs**

```csharp
using DainnUser.Core.Entities;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Address;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for address management.
/// </summary>
public class AddressService : IAddressService
{
    private readonly IUserRepository _userRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressService"/> class.
    /// </summary>
    /// <param name="userRepository">The user repository.</param>
    /// <param name="addressRepository">The address repository.</param>
    /// <param name="unitOfWork">The unit of work.</param>
    public AddressService(
        IUserRepository userRepository,
        IAddressRepository addressRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _addressRepository = addressRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AddressDto>> GetAddressesAsync(Guid userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var addresses = await _addressRepository.GetByUserIdAsync(userId, ct);
        return addresses.Select(MapToDto).ToList();
    }

    /// <inheritdoc/>
    public async Task<AddressDto> GetAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);
        return MapToDto(address);
    }

    /// <inheritdoc/>
    public async Task<AddressDto> AddAddressAsync(Guid userId, AddAddressDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await EnsureUserExistsAsync(userId, ct);

        var now = DateTime.UtcNow;
        var address = new UserAddress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AddressType = dto.AddressType ?? string.Empty,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            StateProvince = dto.StateProvince,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (dto.SetAsDefault)
        {
            await _addressRepository.ClearDefaultForUserAsync(userId, ct);
            address.IsDefault = true;
        }
        else
        {
            var hasAddresses = await _addressRepository.UserHasAddressesAsync(userId, ct);
            address.IsDefault = !hasAddresses;
        }

        await _addressRepository.AddAsync(address, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(address);
    }

    /// <inheritdoc/>
    public async Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);

        if (dto.AddressType is not null)
            address.AddressType = dto.AddressType;
        if (dto.AddressLine1 is not null)
            address.AddressLine1 = dto.AddressLine1;
        if (dto.AddressLine2 is not null)
            address.AddressLine2 = dto.AddressLine2;
        if (dto.City is not null)
            address.City = dto.City;
        if (dto.StateProvince is not null)
            address.StateProvince = dto.StateProvince;
        if (dto.PostalCode is not null)
            address.PostalCode = dto.PostalCode;
        if (dto.Country is not null)
            address.Country = dto.Country;

        address.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToDto(address);
    }

    /// <inheritdoc/>
    public async Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);

        _addressRepository.Remove(address);
        await _unitOfWork.SaveChangesAsync(ct);

        if (address.IsDefault)
        {
            var remaining = await _addressRepository.GetByUserIdAsync(userId, ct);
            var first = remaining.FirstOrDefault();
            if (first is not null)
            {
                first.IsDefault = true;
                first.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }
    }

    /// <inheritdoc/>
    public async Task<AddressDto> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        var address = await GetAddressForUserAsync(userId, addressId, ct);

        await _addressRepository.ClearDefaultForUserAsync(userId, ct);
        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync(ct);
        return MapToDto(address);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken ct)
    {
        var exists = await _userRepository.AnyAsync(u => u.Id == userId, ct);
        if (!exists)
        {
            throw new UserNotFoundException(userId);
        }
    }

    private async Task<UserAddress> GetAddressForUserAsync(Guid userId, Guid addressId, CancellationToken ct)
    {
        var address = await _addressRepository.GetByUserIdAndIdAsync(userId, addressId, ct);
        if (address is null)
        {
            throw new AddressNotFoundException(addressId);
        }
        return address;
    }

    private static AddressDto MapToDto(UserAddress address)
    {
        return new AddressDto
        {
            Id = address.Id,
            AddressType = address.AddressType,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            StateProvince = address.StateProvince,
            PostalCode = address.PostalCode,
            Country = address.Country,
            IsDefault = address.IsDefault,
            CreatedAt = address.CreatedAt,
            UpdatedAt = address.UpdatedAt
        };
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add src/DainnUser.Application/Services/AddressService.cs
git commit -m "feat: add AddressService implementation"
```

---

## Task 7: Register AddressService in ApplicationServiceExtensions

**Files:**
- Modify: `src/DainnUser.Application/ApplicationServiceExtensions.cs`

- [ ] **Step 1: Add IAddressService registration**

Add after line 33 (after `services.AddScoped<IProfileService, ProfileService>();`):
```csharp
        services.AddScoped<IAddressService, AddressService>();
```

- [ ] **Step 2: Commit**

```bash
git add src/DainnUser.Application/ApplicationServiceExtensions.cs
git commit -m "feat: register AddressService in DI"
```

---

## Task 8: Create Address Validators

**Files:**
- Create: `src/DainnUser.Application/Validators/AddAddressDtoValidator.cs`
- Create: `src/DainnUser.Application/Validators/UpdateAddressDtoValidator.cs`

- [ ] **Step 1: Write AddAddressDtoValidator.cs**

```csharp
using System.Text.RegularExpressions;
using DainnUser.Core.Models.Address;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for AddAddressDto.
/// </summary>
public class AddAddressDtoValidator : AbstractValidator<AddAddressDto>
{
    private static readonly Regex PostalCodePattern = new(@"^[a-zA-Z0-9\s\-]{1,20}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="AddAddressDtoValidator"/> class.
    /// </summary>
    public AddAddressDtoValidator()
    {
        RuleFor(x => x.AddressLine1)
            .NotEmpty().WithMessage("Address line 1 is required.")
            .MaximumLength(500).WithMessage("Address line 1 must not exceed 500 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("Country is required.")
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters.")
            .Matches(PostalCodePattern).When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithMessage("Postal code must be alphanumeric with spaces or dashes, max 20 characters.");

        RuleFor(x => x.AddressType)
            .MaximumLength(50).WithMessage("Address type must not exceed 50 characters.");

        RuleFor(x => x.AddressLine2)
            .MaximumLength(500).WithMessage("Address line 2 must not exceed 500 characters.");

        RuleFor(x => x.StateProvince)
            .MaximumLength(100).WithMessage("State/Province must not exceed 100 characters.");
    }
}
```

- [ ] **Step 2: Write UpdateAddressDtoValidator.cs**

```csharp
using System.Text.RegularExpressions;
using DainnUser.Core.Models.Address;
using FluentValidation;

namespace DainnUser.Application.Validators;

/// <summary>
/// Validator for UpdateAddressDto.
/// </summary>
public class UpdateAddressDtoValidator : AbstractValidator<UpdateAddressDto>
{
    private static readonly Regex PostalCodePattern = new(@"^[a-zA-Z0-9\s\-]{1,20}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAddressDtoValidator"/> class.
    /// </summary>
    public UpdateAddressDtoValidator()
    {
        RuleFor(x => x.AddressLine1)
            .MaximumLength(500).WithMessage("Address line 1 must not exceed 500 characters.")
            .When(x => x.AddressLine1 is not null);

        RuleFor(x => x.City)
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.")
            .When(x => x.City is not null);

        RuleFor(x => x.Country)
            .MaximumLength(100).WithMessage("Country must not exceed 100 characters.")
            .When(x => x.Country is not null);

        RuleFor(x => x.PostalCode)
            .MaximumLength(20).WithMessage("Postal code must not exceed 20 characters.")
            .Matches(PostalCodePattern).When(x => !string.IsNullOrWhiteSpace(x.PostalCode))
            .WithMessage("Postal code must be alphanumeric with spaces or dashes, max 20 characters.");

        RuleFor(x => x.AddressType)
            .MaximumLength(50).WithMessage("Address type must not exceed 50 characters.")
            .When(x => x.AddressType is not null);

        RuleFor(x => x.AddressLine2)
            .MaximumLength(500).WithMessage("Address line 2 must not exceed 500 characters.")
            .When(x => x.AddressLine2 is not null);

        RuleFor(x => x.StateProvince)
            .MaximumLength(100).WithMessage("State/Province must not exceed 100 characters.")
            .When(x => x.StateProvince is not null);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add src/DainnUser.Application/Validators/AddAddressDtoValidator.cs src/DainnUser.Application/Validators/UpdateAddressDtoValidator.cs
git commit -m "feat: add address validators"
```

---

## Task 9: Create Address DTOs and Controller

**Files:**
- Create: `src/DainnUser.Api/DTOs/Address/AddressResponse.cs`
- Create: `src/DainnUser.Api/DTOs/Address/AddAddressRequest.cs`
- Create: `src/DainnUser.Api/DTOs/Address/UpdateAddressRequest.cs`
- Create: `src/DainnUser.Api/Controllers/AddressController.cs`

- [ ] **Step 1: Write AddressResponse.cs**

```csharp
namespace DainnUser.Api.DTOs.Address;

/// <summary>
/// API response model for address data.
/// </summary>
public class AddressResponse
{
    public Guid Id { get; set; }
    public string AddressType { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

- [ ] **Step 2: Write AddAddressRequest.cs**

```csharp
using System.ComponentModel.DataAnnotations;

namespace DainnUser.Api.DTOs.Address;

/// <summary>
/// API request model for creating an address.
/// </summary>
public class AddAddressRequest
{
    public string AddressType { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; } = string.Empty;
    public bool SetAsDefault { get; set; }
}
```

- [ ] **Step 3: Write UpdateAddressRequest.cs**

```csharp
namespace DainnUser.Api.DTOs.Address;

/// <summary>
/// API request model for updating an address.
/// </summary>
public class UpdateAddressRequest
{
    public string? AddressType { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
```

- [ ] **Step 4: Write AddressController.cs**

```csharp
using System.Security.Claims;
using DainnUser.Api.DTOs;
using DainnUser.Api.DTOs.Address;
using DainnUser.Application.Validators;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Address;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DainnUser.Api.Controllers;

/// <summary>
/// Controller for address management operations.
/// </summary>
[ApiController]
[Route("api/profile/addresses")]
[Authorize]
public class AddressController : ControllerBase
{
    private readonly IAddressService _addressService;
    private readonly IValidator<AddAddressDto> _addValidator;
    private readonly IValidator<UpdateAddressDto> _updateValidator;
    private readonly ILogger<AddressController> _logger;

    public AddressController(
        IAddressService addressService,
        IValidator<AddAddressDto> addValidator,
        IValidator<UpdateAddressDto> updateValidator,
        ILogger<AddressController> logger)
    {
        _addressService = addressService;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets all addresses for the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AddressResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<AddressResponse>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAddresses(CancellationToken ct)
    {
        var userId = GetUserId();
        var addresses = await _addressService.GetAddressesAsync(userId, ct);
        var response = addresses.Select(MapToResponse).ToList();
        return Ok(ApiResponse<IReadOnlyList<AddressResponse>>.SuccessResponse(response));
    }

    /// <summary>
    /// Gets a specific address by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAddress(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            var address = await _addressService.GetAddressAsync(userId, id, ct);
            return Ok(ApiResponse<AddressResponse>.SuccessResponse(MapToResponse(address)));
        }
        catch (AddressNotFoundException)
        {
            return NotFound(ApiResponse<AddressResponse>.ErrorResponse("Address not found."));
        }
    }

    /// <summary>
    /// Creates a new address.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddAddress([FromBody] AddAddressRequest request, CancellationToken ct)
    {
        var validation = await _addValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<AddressResponse>.ErrorResponse("Validation failed.", errors));
        }

        var userId = GetUserId();
        var dto = new AddAddressDto
        {
            AddressType = request.AddressType,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            City = request.City,
            StateProvince = request.StateProvince,
            PostalCode = request.PostalCode,
            Country = request.Country,
            SetAsDefault = request.SetAsDefault
        };

        var address = await _addressService.AddAddressAsync(userId, dto, ct);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<AddressResponse>.SuccessResponse(MapToResponse(address), "Address created successfully."));
    }

    /// <summary>
    /// Updates an existing address.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAddress(Guid id, [FromBody] UpdateAddressRequest request, CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<AddressResponse>.ErrorResponse("Validation failed.", errors));
        }

        try
        {
            var userId = GetUserId();
            var dto = new UpdateAddressDto
            {
                AddressType = request.AddressType,
                AddressLine1 = request.AddressLine1,
                AddressLine2 = request.AddressLine2,
                City = request.City,
                StateProvince = request.StateProvince,
                PostalCode = request.PostalCode,
                Country = request.Country
            };

            var address = await _addressService.UpdateAddressAsync(userId, id, dto, ct);
            return Ok(ApiResponse<AddressResponse>.SuccessResponse(MapToResponse(address), "Address updated successfully."));
        }
        catch (AddressNotFoundException)
        {
            return NotFound(ApiResponse<AddressResponse>.ErrorResponse("Address not found."));
        }
    }

    /// <summary>
    /// Deletes an address.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAddress(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            await _addressService.DeleteAddressAsync(userId, id, ct);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Address deleted successfully."));
        }
        catch (AddressNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Address not found."));
        }
    }

    /// <summary>
    /// Sets an address as the default.
    /// </summary>
    [HttpPost("{id:guid}/default")]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<AddressResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultAddress(Guid id, CancellationToken ct)
    {
        try
        {
            var userId = GetUserId();
            var address = await _addressService.SetDefaultAddressAsync(userId, id, ct);
            return Ok(ApiResponse<AddressResponse>.SuccessResponse(MapToResponse(address), "Default address set successfully."));
        }
        catch (AddressNotFoundException)
        {
            return NotFound(ApiResponse<AddressResponse>.ErrorResponse("Address not found."));
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static AddressResponse MapToResponse(Core.Models.Address.AddressDto dto)
    {
        return new AddressResponse
        {
            Id = dto.Id,
            AddressType = dto.AddressType,
            AddressLine1 = dto.AddressLine1,
            AddressLine2 = dto.AddressLine2,
            City = dto.City,
            StateProvince = dto.StateProvince,
            PostalCode = dto.PostalCode,
            Country = dto.Country,
            IsDefault = dto.IsDefault,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
}
```

- [ ] **Step 5: Commit**

```bash
git add src/DainnUser.Api/DTOs/Address/AddressResponse.cs src/DainnUser.Api/DTOs/Address/AddAddressRequest.cs src/DainnUser.Api/DTOs/Address/UpdateAddressRequest.cs src/DainnUser.Api/Controllers/AddressController.cs
git commit -m "feat: add AddressController and API DTOs"
```

---

## Task 10: Create Unit Tests for AddressService

**Files:**
- Create: `tests/DainnUser.UnitTests/Services/AddressServiceTests.cs`

- [ ] **Step 1: Write AddressServiceTests.cs**

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add tests/DainnUser.UnitTests/Services/AddressServiceTests.cs
git commit -m "test: add AddressService unit tests"
```

---

## Task 11: Create Unit Tests for Validators

**Files:**
- Create: `tests/DainnUser.UnitTests/Validators/AddAddressDtoValidatorTests.cs`
- Create: `tests/DainnUser.UnitTests/Validators/UpdateAddressDtoValidatorTests.cs`

- [ ] **Step 1: Write AddAddressDtoValidatorTests.cs**

```csharp
using DainnUser.Application.Validators;
using DainnUser.Core.Models.Address;
using FluentAssertions;
using Xunit;

namespace DainnUser.UnitTests.Validators;

public class AddAddressDtoValidatorTests
{
    private readonly AddAddressDtoValidator _validator = new();

    [Fact]
    public void ValidAddress_Passes()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main Street",
            City = "Hanoi",
            Country = "Vietnam",
            PostalCode = "10000"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingAddressLine1_Fails()
    {
        var dto = new AddAddressDto { City = "Hanoi", Country = "Vietnam" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressLine1");
    }

    [Fact]
    public void MissingCity_Fails()
    {
        var dto = new AddAddressDto { AddressLine1 = "123 Main St", Country = "Vietnam" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "City");
    }

    [Fact]
    public void MissingCountry_Fails()
    {
        var dto = new AddAddressDto { AddressLine1 = "123 Main St", City = "Hanoi" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }

    [Fact]
    public void InvalidPostalCode_Fails()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            PostalCode = "invalid!@#$%"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode");
    }

    [Fact]
    public void ValidPostalCodeWithSpaces_Passes()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            PostalCode = "10000"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddressLine1Exceeds500Chars_Fails()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = new string('x', 501),
            City = "Hanoi",
            Country = "Vietnam"
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressLine1");
    }

    [Fact]
    public void AddressTypeExceeds50Chars_Fails()
    {
        var dto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressType = new string('x', 51)
        };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressType");
    }
}
```

- [ ] **Step 2: Write UpdateAddressDtoValidatorTests.cs**

```csharp
using DainnUser.Application.Validators;
using DainnUser.Core.Models.Address;
using FluentAssertions;
using Xunit;

namespace DainnUser.UnitTests.Validators;

public class UpdateAddressDtoValidatorTests
{
    private readonly UpdateAddressDtoValidator _validator = new();

    [Fact]
    public void EmptyDto_Passes()
    {
        var dto = new UpdateAddressDto();

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void PartialUpdate_Passes()
    {
        var dto = new UpdateAddressDto { City = "Ho Chi Minh", AddressLine1 = "456 New St" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void InvalidPostalCode_Fails()
    {
        var dto = new UpdateAddressDto { PostalCode = "invalid!@#" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PostalCode");
    }

    [Fact]
    public void ValidPostalCode_Passes()
    {
        var dto = new UpdateAddressDto { PostalCode = "10000-1234" };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void AddressLine1Exceeds500Chars_Fails()
    {
        var dto = new UpdateAddressDto { AddressLine1 = new string('x', 501) };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AddressLine1");
    }

    [Fact]
    public void CountryExceeds100Chars_Fails()
    {
        var dto = new UpdateAddressDto { Country = new string('x', 101) };

        var result = _validator.Validate(dto);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Country");
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add tests/DainnUser.UnitTests/Validators/AddAddressDtoValidatorTests.cs tests/DainnUser.UnitTests/Validators/UpdateAddressDtoValidatorTests.cs
git commit -m "test: add address validator unit tests"
```

---

## Task 12: Create Integration Tests

**Files:**
- Create: `tests/DainnUser.IntegrationTests/Services/AddressServiceIntegrationTests.cs`
- Create: `tests/DainnUser.IntegrationTests/Api/AddressControllerTests.cs`

- [ ] **Step 1: Write AddressServiceIntegrationTests.cs**

```csharp
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Models.Address;
using DainnUser.Infrastructure.Repositories;
using DainnUser.IntegrationTests.TestFixtures;
using FluentAssertions;
using Xunit;

namespace DainnUser.IntegrationTests.Services;

public class AddressServiceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public AddressServiceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
        _fixture.ClearDatabase();
    }

    private User CreateTestUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid()}@example.com",
            Username = $"user-{Guid.NewGuid():N}".Substring(0, 20),
            PasswordHash = "hash",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _fixture.DbContext.Users.Add(user);
        _fixture.DbContext.SaveChanges();
        return user;
    }

    [Fact]
    public async Task AddAndRetrieveAddress_Success()
    {
        var user = CreateTestUser();
        var repo = new AddressRepository(_fixture.DbContext);
        var unitOfWork = new UnitOfWork(_fixture.DbContext);
        var service = new AddressService(new UserRepository(_fixture.DbContext), repo, unitOfWork);

        var addDto = new AddAddressDto
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            AddressType = "Home"
        };

        var added = await service.AddAddressAsync(user.Id, addDto);

        added.AddressLine1.Should().Be("123 Main St");
        added.IsDefault.Should().BeTrue();

        var addresses = await service.GetAddressesAsync(user.Id);
        addresses.Should().HaveCount(1);
    }

    [Fact]
    public async Task SetDefault_PromotesNewDefault()
    {
        var user = CreateTestUser();
        var repo = new AddressRepository(_fixture.DbContext);
        var unitOfWork = new UnitOfWork(_fixture.DbContext);
        var service = new AddressService(new UserRepository(_fixture.DbContext), repo, unitOfWork);

        var first = await service.AddAddressAsync(user.Id, new AddAddressDto { AddressLine1 = "First", City = "Hanoi", Country = "Vietnam" });
        var second = await service.AddAddressAsync(user.Id, new AddAddressDto { AddressLine1 = "Second", City = "Hanoi", Country = "Vietnam", SetAsDefault = true });

        var firstAfter = await service.GetAddressAsync(user.Id, first.Id);
        var secondAfter = await service.GetAddressAsync(user.Id, second.Id);

        firstAfter.IsDefault.Should().BeFalse();
        secondAfter.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteDefault_PromotesRemaining()
    {
        var user = CreateTestUser();
        var repo = new AddressRepository(_fixture.DbContext);
        var unitOfWork = new UnitOfWork(_fixture.DbContext);
        var service = new AddressService(new UserRepository(_fixture.DbContext), repo, unitOfWork);

        var first = await service.AddAddressAsync(user.Id, new AddAddressDto { AddressLine1 = "First", City = "Hanoi", Country = "Vietnam" });
        var second = await service.AddAddressAsync(user.Id, new AddAddressDto { AddressLine1 = "Second", City = "Hanoi", Country = "Vietnam" });

        await service.DeleteAddressAsync(user.Id, first.Id);

        var secondAfter = await service.GetAddressAsync(user.Id, second.Id);
        secondAfter.IsDefault.Should().BeTrue();
    }
}
```

- [ ] **Step 2: Write AddressControllerTests.cs**

```csharp
using DainnUser.Api.Controllers;
using DainnUser.Application.Validators;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Address;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace DainnUser.IntegrationTests.Api;

public class AddressControllerTests
{
    private readonly Mock<IAddressService> _serviceMock = new();
    private readonly AddressController _controller;
    private readonly Guid _userId = Guid.NewGuid();

    public AddressControllerTests()
    {
        var loggerMock = new Mock<ILogger<AddressController>>();
        _controller = new AddressController(
            _serviceMock.Object,
            new AddAddressRequestValidator(),
            new UpdateAddressRequestValidator(),
            loggerMock.Object);

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, _userId.ToString()) };
        var identity = new ClaimsIdentity(claims, "Test");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task GetAddresses_ReturnsOk()
    {
        var addresses = new List<AddressDto>
        {
            new() { Id = Guid.NewGuid(), AddressLine1 = "123 Main St", City = "Hanoi", Country = "Vietnam", IsDefault = true },
            new() { Id = Guid.NewGuid(), AddressLine1 = "456 Work St", City = "HCMC", Country = "Vietnam", IsDefault = false }
        };
        _serviceMock.Setup(x => x.GetAddressesAsync(_userId, default)).ReturnsAsync(addresses);

        var result = await _controller.GetAddresses(default);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task AddAddress_ValidRequest_ReturnsCreated()
    {
        var request = new DainnUser.Api.DTOs.Address.AddAddressRequest
        {
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam"
        };
        var created = new AddressDto
        {
            Id = Guid.NewGuid(),
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            IsDefault = true
        };
        _serviceMock.Setup(x => x.AddAddressAsync(_userId, It.IsAny<AddAddressDto>(), default)).ReturnsAsync(created);

        var result = await _controller.AddAddress(request, default);

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task AddAddress_InvalidRequest_ReturnsBadRequest()
    {
        var request = new DainnUser.Api.DTOs.Address.AddAddressRequest
        {
            AddressLine1 = "",
            City = "",
            Country = ""
        };

        var result = await _controller.AddAddress(request, default);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DeleteAddress_NotFound_Returns404()
    {
        _serviceMock.Setup(x => x.DeleteAddressAsync(_userId, It.IsAny<Guid>(), default))
            .ThrowsAsync(new Core.Exceptions.AddressNotFoundException(Guid.NewGuid()));

        var result = await _controller.DeleteAddress(Guid.NewGuid(), default);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task SetDefault_ReturnsOk()
    {
        var addressId = Guid.NewGuid();
        var address = new AddressDto
        {
            Id = addressId,
            AddressLine1 = "123 Main St",
            City = "Hanoi",
            Country = "Vietnam",
            IsDefault = true
        };
        _serviceMock.Setup(x => x.SetDefaultAddressAsync(_userId, addressId, default)).ReturnsAsync(address);

        var result = await _controller.SetDefaultAddress(addressId, default);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add tests/DainnUser.IntegrationTests/Services/AddressServiceIntegrationTests.cs tests/DainnUser.IntegrationTests/Api/AddressControllerTests.cs
git commit -m "test: add address integration tests"
```

---

## Task 13: Build and Test

**Files:** (verification only)

- [ ] **Step 1: Build solution**

Run: `dotnet build dainn-user.sln -c Release`
Expected: 0 errors

- [ ] **Step 2: Run unit tests**

Run: `dotnet test dainn-user.sln --filter "FullyQualifiedName~AddressService|FullyQualifiedName~AddAddressDtoValidator|FullyQualifiedName~UpdateAddressDtoValidator" --verbosity normal`
Expected: All pass

- [ ] **Step 3: Run integration tests**

Run: `dotnet test dainn-user.sln --filter "FullyQualifiedName~AddressServiceIntegration|FullyQualifiedName~AddressController" --verbosity normal`
Expected: All pass

- [ ] **Step 4: Final commit if all pass**

```bash
git commit --allow-empty -m "chore: verify PSA-77 build and tests pass"
```

---

## Commit Checkpoints

| # | After | Message |
|---|-------|---------|
| 1 | Task 1 | `feat: add AddressNotFoundException` |
| 2 | Task 2 | `feat: add address DTOs` |
| 3 | Task 3 | `feat: add IAddressRepository interface` |
| 4 | Task 4 | `feat: add AddressRepository implementation` |
| 5 | Task 5 | `feat: add IAddressRepository to IUnitOfWork` |
| 6 | Task 6 | `feat: add AddressService implementation` |
| 7 | Task 7 | `feat: register AddressService in DI` |
| 8 | Task 8 | `feat: add address validators` |
| 9 | Task 9 | `feat: add AddressController and API DTOs` |
| 10 | Task 10 | `test: add AddressService unit tests` |
| 11 | Task 11 | `test: add address validator unit tests` |
| 12 | Task 12 | `test: add address integration tests` |
| 13 | Task 13 | `chore: verify PSA-77 build and tests pass` |