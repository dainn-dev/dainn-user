using System.Security.Claims;
using DainnUser.Api.DTOs;
using DainnUser.Api.DTOs.Contact;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Contact;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DainnUser.Api.Controllers;

/// <summary>
/// Controller for contact management operations.
/// </summary>
[ApiController]
[Route("api/profile/contacts")]
[Authorize]
public class ContactController : ControllerBase
{
    private readonly IContactService _contactService;
    private readonly IValidator<AddContactDto> _addValidator;
    private readonly IValidator<UpdateContactDto> _updateValidator;

    public ContactController(
        IContactService contactService,
        IValidator<AddContactDto> addValidator,
        IValidator<UpdateContactDto> updateValidator)
    {
        _contactService = contactService;
        _addValidator = addValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>
    /// Gets all contacts for the authenticated user.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContactResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ContactResponse>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetContacts(CancellationToken ct)
    {
        var contacts = await _contactService.GetContactsAsync(GetUserId(), ct);
        return Ok(ApiResponse<IReadOnlyList<ContactResponse>>.SuccessResponse(contacts.Select(MapToResponse).ToList()));
    }

    /// <summary>
    /// Gets a specific contact by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetContact(Guid id, CancellationToken ct)
    {
        try
        {
            var contact = await _contactService.GetContactAsync(GetUserId(), id, ct);
            return Ok(ApiResponse<ContactResponse>.SuccessResponse(MapToResponse(contact)));
        }
        catch (ContactNotFoundException)
        {
            return NotFound(ApiResponse<ContactResponse>.ErrorResponse("Contact not found."));
        }
    }

    /// <summary>
    /// Creates a new contact.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddContact([FromBody] AddContactRequest request, CancellationToken ct)
    {
        var dto = new AddContactDto
        {
            ContactType = request.ContactType,
            ContactValue = request.ContactValue,
            SetAsPrimary = request.SetAsPrimary
        };

        var validation = await _addValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<ContactResponse>.ErrorResponse(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        var contact = await _contactService.AddContactAsync(GetUserId(), dto, ct);
        return StatusCode(StatusCodes.Status201Created,
            ApiResponse<ContactResponse>.SuccessResponse(MapToResponse(contact), "Contact created successfully."));
    }

    /// <summary>
    /// Updates an existing contact.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateContact(Guid id, [FromBody] UpdateContactRequest request, CancellationToken ct)
    {
        var dto = new UpdateContactDto
        {
            ContactType = request.ContactType,
            ContactValue = request.ContactValue
        };

        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<ContactResponse>.ErrorResponse(
                "Validation failed.",
                validation.Errors.Select(e => e.ErrorMessage).ToList()));
        }

        try
        {
            var contact = await _contactService.UpdateContactAsync(GetUserId(), id, dto, ct);
            return Ok(ApiResponse<ContactResponse>.SuccessResponse(MapToResponse(contact), "Contact updated successfully."));
        }
        catch (ContactNotFoundException)
        {
            return NotFound(ApiResponse<ContactResponse>.ErrorResponse("Contact not found."));
        }
    }

    /// <summary>
    /// Deletes a contact.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteContact(Guid id, CancellationToken ct)
    {
        try
        {
            await _contactService.DeleteContactAsync(GetUserId(), id, ct);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Contact deleted successfully."));
        }
        catch (ContactNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Contact not found."));
        }
    }

    /// <summary>
    /// Sets a contact as primary for its type.
    /// </summary>
    [HttpPost("{id:guid}/primary")]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryContact(Guid id, CancellationToken ct)
    {
        try
        {
            var contact = await _contactService.SetPrimaryContactAsync(GetUserId(), id, ct);
            return Ok(ApiResponse<ContactResponse>.SuccessResponse(MapToResponse(contact), "Primary contact set successfully."));
        }
        catch (ContactNotFoundException)
        {
            return NotFound(ApiResponse<ContactResponse>.ErrorResponse("Contact not found."));
        }
    }

    /// <summary>
    /// Sends a verification code for a contact.
    /// </summary>
    [HttpPost("{id:guid}/send-verification")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status501NotImplemented)]
    public async Task<IActionResult> SendVerification(Guid id, CancellationToken ct)
    {
        try
        {
            await _contactService.SendVerificationCodeAsync(GetUserId(), id, ct);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Verification code sent successfully."));
        }
        catch (ContactNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Contact not found."));
        }
        catch (TooManyVerificationAttemptsException)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, ApiResponse<object>.ErrorResponse("Too many attempts. Try again later."));
        }
        catch (NotSupportedException ex)
        {
            return StatusCode(StatusCodes.Status501NotImplemented, ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Verifies a contact with a code.
    /// </summary>
    [HttpPost("{id:guid}/verify")]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ContactResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyContact(Guid id, [FromBody] VerifyContactRequest request, CancellationToken ct)
    {
        try
        {
            var contact = await _contactService.VerifyContactAsync(GetUserId(), id, request.Code, ct);
            return Ok(ApiResponse<ContactResponse>.SuccessResponse(MapToResponse(contact), "Contact verified successfully."));
        }
        catch (ContactNotFoundException)
        {
            return NotFound(ApiResponse<ContactResponse>.ErrorResponse("Contact not found."));
        }
        catch (InvalidVerificationCodeException)
        {
            return BadRequest(ApiResponse<ContactResponse>.ErrorResponse("The verification code is invalid or has expired."));
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static ContactResponse MapToResponse(ContactDto dto)
    {
        return new ContactResponse
        {
            Id = dto.Id,
            ContactType = dto.ContactType,
            ContactValue = dto.ContactValue,
            IsVerified = dto.IsVerified,
            IsPrimary = dto.IsPrimary,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
}
