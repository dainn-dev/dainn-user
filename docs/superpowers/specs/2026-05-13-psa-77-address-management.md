# PSA-77: Address Management Design

**Date:** 2026-05-13
**Status:** Approved
**Linear Issue:** [PSA-77](https://linear.app/psa-app/issue/PSA-77/implement-address-management)

## Overview

Implement address management for DainnUser. Addresses are sub-resources of user profile (`api/profile/addresses`). Follows existing library patterns for service, validator, controller, and tests.

## Goals

- User can manage multiple addresses (Home, Work, Shipping, Billing, etc.)
- Addresses have default flag — one default at a time
- Address validation via FluentValidation
- Comprehensive unit and integration tests

## Current State

**Already exists:**
- `UserAddress` entity with fields: Id, UserId, AddressType, AddressLine1, AddressLine2, City, StateProvince, PostalCode, Country, IsDefault, CreatedAt, UpdatedAt, User navigation
- `UserAddressConfiguration` with table name, column max lengths, indexes
- `DbSet<UserAddress> UserAddresses` in DainnUserDbContext
- Pattern from `ProfileService`, `ProfileController` for service/controller structure

**Missing:**
- `IAddressRepository` interface and `AddressRepository` implementation
- IAddressService interface
- AddressService implementation
- DTOs (Add/Update/Get)
- Validators (Add/Update)
- AddressController
- Tests (unit + integration)

## Design

### Project Structure

```
DainnUser.Core/
├── Entities/UserAddress.cs                 (existing)
├── Interfaces/Repositories/
│   └── IAddressRepository.cs               (NEW)
├── Interfaces/Services/IAddressService.cs  (NEW)
└── Models/Address/
    ├── AddressDto.cs                       (NEW)
    ├── AddAddressDto.cs                    (NEW)
    └── UpdateAddressDto.cs                 (NEW)

DainnUser.Application/
├── Services/AddressService.cs              (NEW)
└── Validators/
    ├── AddAddressDtoValidator.cs           (NEW)
    └── UpdateAddressDtoValidator.cs        (NEW)

DainnUser.Infrastructure/
├── Data/Repositories/
│   └── AddressRepository.cs                (NEW)

DainnUser.Api/
├── DTOs/Address/
│   ├── AddressResponse.cs                  (NEW)
│   ├── AddAddressRequest.cs                (NEW)
│   └── UpdateAddressRequest.cs             (NEW)
└── Controllers/
    └── AddressController.cs                (NEW)

tests/
├── DainnUser.UnitTests/Services/AddressServiceTests.cs
├── DainnUser.UnitTests/Validators/AddAddressDtoValidatorTests.cs
├── DainnUser.UnitTests/Validators/UpdateAddressDtoValidatorTests.cs
├── DainnUser.IntegrationTests/Api/AddressControllerTests.cs
└── DainnUser.IntegrationTests/Services/AddressServiceIntegrationTests.cs
```

### IAddressService Interface

```csharp
public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetAddressesAsync(Guid userId, CancellationToken ct = default);
    Task<AddressDto> GetAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default);
    Task<AddressDto> AddAddressAsync(Guid userId, AddAddressDto dto, CancellationToken ct = default);
    Task<AddressDto> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressDto dto, CancellationToken ct = default);
    Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default);
    Task<AddressDto> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken ct = default);
}
```

### AddressDto (for return)

```csharp
public class AddressDto
{
    public Guid Id { get; set; }
    public string AddressType { get; set; }
    public string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### AddAddressDto

```csharp
public class AddAddressDto
{
    public string AddressType { get; set; }  // optional, default ""
    public string AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string City { get; set; }
    public string? StateProvince { get; set; }
    public string? PostalCode { get; set; }
    public string Country { get; set; }
    public bool SetAsDefault { get; set; }  // optional, false
}
```

### UpdateAddressDto

```csharp
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

### API Endpoints

Route base: `api/profile/addresses`

| Method | Path | Auth | Description |
|--------|------|------|-------------|
| GET | `api/profile/addresses` | Authorized | List all addresses for user |
| GET | `api/profile/addresses/{id}` | Authorized | Get single address |
| POST | `api/profile/addresses` | Authorized | Add new address |
| PUT | `api/profile/addresses/{id}` | Authorized | Update address |
| DELETE | `api/profile/addresses/{id}` | Authorized | Delete address |
| POST | `api/profile/addresses/{id}/default` | Authorized | Set as default |

### Validation Rules

**AddAddressDtoValidator:**
- `AddressLine1`: required, max 500 chars
- `City`: required, max 100 chars
- `Country`: required, max 100 chars
- `PostalCode`: optional, max 20 chars, regex `^[a-zA-Z0-9\s\-]{1,20}$` if provided
- `AddressType`: max 50 chars

**UpdateAddressDtoValidator:**
- `AddressLine1`: max 500 chars if provided
- `City`: max 100 chars if provided
- `Country`: max 100 chars if provided
- `PostalCode`: regex `^[a-zA-Z0-9\s\-]{1,20}$` if provided
- `AddressType`: max 50 chars if provided

### Key Behaviors

1. **Default management:**
   - First address created becomes default (IsDefault = true)
   - `SetDefaultAddressAsync`: unmarks all user's addresses as default, then marks the specified one
   - Deleting an address that is default: if other addresses exist, promote the first remaining address to default

2. **Address types:**
   - Free-form string, max 50 chars
   - Common examples: "Home", "Work", "Shipping", "Billing", "Parents' House", etc.
   - No predefined enum — user can type any value

3. **User ownership:**
   - All methods require userId (from JWT token)
   - Cannot access other users' addresses
   - Throws `UserNotFoundException` if user doesn't exist
   - Throws `AddressNotFoundException` if address doesn't exist or doesn't belong to user

4. **Repository:**
   - Use existing `IAddressRepository` pattern if it exists, otherwise create
   - Need: get by userId, get by userId + addressId, add, update, delete

### Exceptions

- `UserNotFoundException(Guid userId)` — user does not exist
- `AddressNotFoundException(Guid addressId)` — address does not exist or doesn't belong to user

### Service Registration

In `ApplicationServiceExtensions.cs`:
```csharp
services.AddScoped<IAddressService, AddressService>();
```

In `RepositoryServiceExtensions.cs`:
```csharp
services.AddScoped<IAddressRepository, AddressRepository>();
```

### Testing Coverage

**Unit Tests (AddressService):**
- GetAddresses returns empty list for user with no addresses
- GetAddresses returns all addresses for user
- AddAddress sets first address as default
- AddAddress respects SetAsDefault flag
- UpdateAddress modifies address fields
- UpdateAddress throws for non-existent address
- DeleteAddress removes address
- DeleteAddress of default promotes another
- SetDefaultAddress unmarks others and marks new default
- SetDefaultAddress throws for non-existent address
- All methods throw UserNotFoundException for invalid userId

**Unit Tests (Validators):**
- Valid address passes
- Missing required fields fails
- PostalCode format validation
- Max length validation

**Integration Tests:**
- GET /api/profile/addresses returns 401 without token
- GET /api/profile/addresses returns addresses for authenticated user
- POST /api/profile/addresses creates address
- DELETE removes address
- Full flow: create, update, set default, delete

## Non-Goals

- Admin endpoints for address management (out of scope for this iteration)
- Address geocoding or validation service integration
- Multiple default addresses per type (single default per user)
- Bulk address import/export

## Dependencies

- Uses existing `IUserRepository`, `IAddressRepository` (or creates new address repo)
- Uses existing `IUnitOfWork`
- Uses existing `UserNotFoundException`
- Uses existing FluentValidation pattern
- Uses existing `ApiResponse<T>` pattern for controller responses

## Risks & Mitigations

**Risk:** IAddressRepository doesn't exist and needs to be created
**Mitigation:** Create IAddressRepository + AddressRepository following existing UserRepository pattern

**Risk:** Concurrent default updates (race condition)
**Mitigation:** Use UnitOfWork transaction for SetDefaultAddressAsync