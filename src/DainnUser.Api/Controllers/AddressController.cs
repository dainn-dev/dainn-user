using System.Security.Claims;
using DainnUser.Api.DTOs;
using DainnUser.Api.DTOs.Address;
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

        var validation = await _addValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<AddressResponse>.ErrorResponse("Validation failed.", errors));
        }

        var userId = GetUserId();
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

        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<AddressResponse>.ErrorResponse("Validation failed.", errors));
        }

        try
        {
            var userId = GetUserId();
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

    private static AddressResponse MapToResponse(AddressDto dto)
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
