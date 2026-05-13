# Contact Information Management — Design Specification

**Issue:** PSA-78  
**Date:** 2026-05-13  
**Status:** Approved

---

## Overview

Implement contact information management for the DainnUser library, allowing users to store, manage, and verify multiple contact methods (phone, email, social media handles). This feature mirrors the recently completed address management (PSA-77) architecture and adds verification capabilities for contact validation.

---

## Requirements

### Functional Requirements

1. **CRUD Operations:**
   - Add contact (phone, email, WhatsApp, Telegram, etc.)
   - Update contact
   - Delete contact
   - List all contacts for a user
   - Get specific contact by ID

2. **Contact Types:**
   - Phone (mobile, home, work)
   - Email (personal, work)
   - Social media (WhatsApp, Telegram, Skype, etc.)

3. **Primary Contact:**
   - One contact per type can be marked as primary
   - First contact of a type auto-becomes primary
   - Setting a new primary clears the old one

4. **Contact Verification:**
   - Generate verification code (6-digit OTP)
   - Send code via appropriate channel (SMS for phone, email for email)
   - Verify code to mark contact as verified
   - Verification codes expire after 15 minutes
   - Codes stored hashed (SHA-256)

### Non-Functional Requirements

1. **Security:**
   - Input validation (phone format, email format)
   - Authorization (users can only manage their own contacts)
   - Verification codes hashed at rest
   - Rate limiting on verification code sending

2. **Extensibility:**
   - Pluggable verification senders (consumers implement for SMS/etc.)
   - Support for new contact types without code changes

3. **Consistency:**
   - Follow existing address management patterns
   - Use same repository/service/controller structure
   - Consistent error handling and validation

---

## Architecture

### Layer Structure

```
┌─────────────────────────────────────────────────────────────┐
│ API Layer (DainnUser.Api)                                    │
│ - ContactController                                          │
│ - DTOs: AddContactRequest, UpdateContactRequest,            │
│         ContactResponse                                      │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Application Layer (DainnUser.Application)                    │
│ - ContactService (implements IContactService)                │
│ - Validators: AddContactDtoValidator, UpdateContactDtoValidator │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Infrastructure Layer (DainnUser.Infrastructure)              │
│ - ContactRepository (implements IContactRepository)          │
│ - EmailContactVerificationSender                             │
│ - SmsContactVerificationSender (stub/interface)              │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│ Core Layer (DainnUser.Core)                                  │
│ - Entities: UserContact (exists), UserToken (extend)         │
│ - Interfaces: IContactRepository, IContactService,           │
│               IContactVerificationSender                     │
│ - DTOs: ContactDto, AddContactDto, UpdateContactDto          │
│ - Exceptions: ContactNotFoundException,                      │
│               InvalidVerificationCodeException               │
└─────────────────────────────────────────────────────────────┘
```

---

## Data Model

### UserContact Entity (Existing)

Already exists in `DainnUser.Core.Entities.UserContact`:

```csharp
public class UserContact
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string ContactType { get; set; }      // "Phone", "Email", "WhatsApp", etc.
    public string ContactValue { get; set; }     // actual phone/email/handle
    public bool IsVerified { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; }
}
```

**No changes needed to UserContact.**

### UserToken Entity (Extend)

Add `ContactId` column to support contact verification tokens:

```csharp
public class UserToken
{
    // ... existing fields ...
    public Guid? ContactId { get; set; }  // NEW: FK to UserContact (nullable)
    
    // Navigation property
    public UserContact? Contact { get; set; }  // NEW
}
```

**Migration:** Add nullable `ContactId` column with FK constraint to `UserContacts` table.

### Verification Token Storage

- **TokenType:** `ContactVerification` (new enum value in `TokenType`)
- **TokenValue:** SHA-256 hash of the 6-digit OTP
- **ContactId:** Set to the contact being verified
- **ExpiresAt:** 15 minutes from generation
- **IsUsed:** Marked true after successful verification

---

## Core Interfaces

### IContactRepository

```csharp
public interface IContactRepository : IRepository<UserContact>
{
    Task<IEnumerable<UserContact>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserContact?> GetByUserIdAndIdAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    Task<UserContact?> GetPrimaryForUserAndTypeAsync(Guid userId, string contactType, CancellationToken ct = default);
    Task ClearPrimaryForUserAndTypeAsync(Guid userId, string contactType, CancellationToken ct = default);
    Task<bool> UserHasContactsAsync(Guid userId, CancellationToken ct = default);
}
```

### IContactService

```csharp
public interface IContactService
{
    // CRUD
    Task<IReadOnlyList<ContactDto>> GetContactsAsync(Guid userId, CancellationToken ct = default);
    Task<ContactDto> GetContactAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    Task<ContactDto> AddContactAsync(Guid userId, AddContactDto dto, CancellationToken ct = default);
    Task<ContactDto> UpdateContactAsync(Guid userId, Guid contactId, UpdateContactDto dto, CancellationToken ct = default);
    Task DeleteContactAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    
    // Primary management
    Task<ContactDto> SetPrimaryContactAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    
    // Verification
    Task SendVerificationCodeAsync(Guid userId, Guid contactId, CancellationToken ct = default);
    Task<ContactDto> VerifyContactAsync(Guid userId, Guid contactId, string code, CancellationToken ct = default);
}
```

### IContactVerificationSender

```csharp
public interface IContactVerificationSender
{
    string ContactType { get; }  // "Phone", "Email", etc.
    Task SendVerificationCodeAsync(string contactValue, string code, CancellationToken ct = default);
}
```

**Implementations:**
- `EmailContactVerificationSender` — uses existing `IEmailService`
- `SmsContactVerificationSender` — stub/abstract; consumer wires Twilio/etc.
- Consumers register implementations in DI per contact type

---

## DTOs

### ContactDto (Read)

```csharp
public class ContactDto
{
    public Guid Id { get; set; }
    public string ContactType { get; set; }
    public string ContactValue { get; set; }
    public bool IsVerified { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### AddContactDto (Create)

```csharp
public class AddContactDto
{
    public string ContactType { get; set; }      // Required
    public string ContactValue { get; set; }     // Required
    public bool SetAsPrimary { get; set; }       // Default: false
}
```

### UpdateContactDto (Update)

```csharp
public class UpdateContactDto
{
    public string? ContactType { get; set; }     // Optional
    public string? ContactValue { get; set; }    // Optional
}
```

**Note:** Updating `ContactValue` resets `IsVerified` to false (requires re-verification).

---

## Validation Rules

### AddContactDto

| Field | Rules |
|---|---|
| `ContactType` | Required, max 50 chars |
| `ContactValue` | Required, max 256 chars, format validation based on type |

**Format Validation:**
- **Phone types** (Phone, Mobile, WhatsApp): E.164 regex `^\+?[1-9]\d{1,14}$`
- **Email types** (Email): Standard email regex
- **Social types** (Telegram, Skype, etc.): Non-empty, alphanumeric + underscores

### UpdateContactDto

Same rules as Add, but all fields optional. At least one field must be provided.

---

## Service Implementation

### ContactService

**Key behaviors:**

1. **Add Contact:**
   - Validate input
   - If `SetAsPrimary=true`, clear existing primary for that type
   - If user has no contacts of this type, auto-set as primary
   - Create contact with `IsVerified=false`

2. **Update Contact:**
   - If `ContactValue` changes, reset `IsVerified=false`
   - If `ContactType` changes, handle primary flag (clear old type's primary if this was primary)

3. **Delete Contact:**
   - If deleting primary contact, promote another contact of same type to primary
   - Remove associated verification tokens

4. **Set Primary:**
   - Clear existing primary for the contact type
   - Set new contact as primary

5. **Send Verification Code:**
   - Generate 6-digit OTP
   - Hash with SHA-256
   - Store in `UserToken` (TokenType=ContactVerification, ContactId set, expires in 15 min)
   - Resolve `IContactVerificationSender` for contact type
   - Send code via sender
   - Rate limit: max 3 codes per contact per hour

6. **Verify Contact:**
   - Look up active token by ContactId + TokenType
   - Verify not expired, not used
   - Compare hash
   - Mark contact `IsVerified=true`
   - Mark token `IsUsed=true`

---

## API Endpoints

### ContactController

**Base route:** `/api/profile/contacts`  
**Authorization:** All endpoints require `[Authorize]`

| Method | Route | Description | Request | Response |
|---|---|---|---|---|
| `GET` | `/` | List all contacts | — | `ApiResponse<IReadOnlyList<ContactResponse>>` |
| `GET` | `/{id:guid}` | Get contact by ID | — | `ApiResponse<ContactResponse>` |
| `POST` | `/` | Add new contact | `AddContactRequest` | `ApiResponse<ContactResponse>` (201) |
| `PUT` | `/{id:guid}` | Update contact | `UpdateContactRequest` | `ApiResponse<ContactResponse>` |
| `DELETE` | `/{id:guid}` | Delete contact | — | `ApiResponse<object>` |
| `POST` | `/{id:guid}/primary` | Set as primary | — | `ApiResponse<ContactResponse>` |
| `POST` | `/{id:guid}/send-verification` | Send verification code | — | `ApiResponse<object>` |
| `POST` | `/{id:guid}/verify` | Verify contact | `{ "code": "123456" }` | `ApiResponse<ContactResponse>` |

**Error Responses:**
- `400` — Validation failed
- `401` — Unauthorized
- `404` — Contact not found
- `429` — Too many verification requests

---

## Exceptions

### ContactNotFoundException

```csharp
public class ContactNotFoundException : Exception
{
    public Guid ContactId { get; }
    public ContactNotFoundException(Guid contactId) 
        : base($"Contact with ID '{contactId}' was not found.")
    {
        ContactId = contactId;
    }
}
```

### InvalidVerificationCodeException

```csharp
public class InvalidVerificationCodeException : Exception
{
    public InvalidVerificationCodeException() 
        : base("The verification code is invalid or has expired.")
    {
    }
}
```

---

## Security Considerations

### Input Validation

- Phone numbers: E.164 format validation
- Email addresses: RFC 5322 validation
- SQL injection: Prevented by EF Core parameterized queries
- XSS: API returns JSON (no HTML rendering)

### Authorization

- Users can only access their own contacts (userId from JWT claims)
- No admin endpoints for contact management (privacy)

### Verification Code Security

- Codes stored hashed (SHA-256), never plain text
- 15-minute expiration
- One-time use (marked `IsUsed=true` after verification)
- Rate limiting: max 3 codes per contact per hour

### Rate Limiting

Add rate limit rule for verification endpoints:
- `/api/profile/contacts/*/send-verification`: 3 requests per 60 minutes per user
- `/api/profile/contacts/*/verify`: 10 requests per 60 minutes per user (allows retries)

---

## Testing Strategy

### Unit Tests

**ContactService:**
- `GetContactsAsync_ReturnsAllUserContacts`
- `GetContactAsync_ReturnsContact_WhenExists`
- `GetContactAsync_ThrowsContactNotFoundException_WhenNotFound`
- `AddContactAsync_CreatesContact_WithCorrectData`
- `AddContactAsync_SetsAsPrimary_WhenSetAsPrimaryTrue`
- `AddContactAsync_AutoSetsPrimary_WhenFirstContactOfType`
- `UpdateContactAsync_UpdatesContact_WithValidData`
- `UpdateContactAsync_ResetsIsVerified_WhenContactValueChanges`
- `DeleteContactAsync_RemovesContact`
- `DeleteContactAsync_PromotesAnotherToPrimary_WhenDeletingPrimary`
- `SetPrimaryContactAsync_SetsPrimary_AndClearsOldPrimary`
- `SendVerificationCodeAsync_GeneratesAndSendsCode`
- `SendVerificationCodeAsync_ThrowsRateLimitException_WhenTooManyRequests`
- `VerifyContactAsync_MarksVerified_WithValidCode`
- `VerifyContactAsync_ThrowsInvalidVerificationCodeException_WithInvalidCode`
- `VerifyContactAsync_ThrowsInvalidVerificationCodeException_WhenExpired`

**Validators:**
- `AddContactDtoValidator_Validates_RequiredFields`
- `AddContactDtoValidator_Validates_PhoneFormat`
- `AddContactDtoValidator_Validates_EmailFormat`
- `UpdateContactDtoValidator_Validates_OptionalFields`

### Integration Tests

**ContactController:**
- `GetContacts_ReturnsAllContacts_ForAuthenticatedUser`
- `GetContact_ReturnsContact_WhenExists`
- `GetContact_Returns404_WhenNotFound`
- `AddContact_CreatesContact_WithValidData`
- `AddContact_Returns400_WithInvalidData`
- `UpdateContact_UpdatesContact_WithValidData`
- `UpdateContact_Returns404_WhenNotFound`
- `DeleteContact_RemovesContact`
- `SetPrimaryContact_SetsPrimary`
- `SendVerificationCode_SendsCode`
- `VerifyContact_MarksVerified_WithValidCode`
- `VerifyContact_Returns400_WithInvalidCode`

### Security Tests

- `ContactVerification_CodesStoredHashed`
- `ContactVerification_CodesExpireAfter15Minutes`
- `ContactVerification_CodesAreOneTimeUse`
- `ContactVerification_RateLimitEnforced`
- `ContactAccess_RestrictedToOwner`

---

## Implementation Checklist

### Core Layer
- [ ] Add `ContactId` to `UserToken` entity
- [ ] Add `ContactVerification` to `TokenType` enum
- [ ] Create `IContactRepository` interface
- [ ] Create `IContactService` interface
- [ ] Create `IContactVerificationSender` interface
- [ ] Create `ContactDto`, `AddContactDto`, `UpdateContactDto`
- [ ] Create `ContactNotFoundException`
- [ ] Create `InvalidVerificationCodeException`

### Infrastructure Layer
- [ ] Create migration for `UserToken.ContactId` column
- [ ] Implement `ContactRepository`
- [ ] Implement `EmailContactVerificationSender`
- [ ] Create stub `SmsContactVerificationSender`
- [ ] Register services in DI

### Application Layer
- [ ] Implement `ContactService`
- [ ] Implement `AddContactDtoValidator`
- [ ] Implement `UpdateContactDtoValidator`
- [ ] Register validators in DI

### API Layer
- [ ] Create `AddContactRequest`, `UpdateContactRequest`, `ContactResponse`
- [ ] Implement `ContactController`
- [ ] Add rate limiting rules for verification endpoints

### Tests
- [ ] Unit tests for `ContactService`
- [ ] Unit tests for validators
- [ ] Integration tests for `ContactController`
- [ ] Security tests for verification flow

### Documentation
- [ ] Update README with contact management features
- [ ] Add API documentation for contact endpoints
- [ ] Document verification sender extensibility

---

## Migration Path

### Database Migration

```sql
-- Add ContactId column to UserTokens
ALTER TABLE UserTokens 
ADD ContactId uniqueidentifier NULL;

-- Add FK constraint
ALTER TABLE UserTokens
ADD CONSTRAINT FK_UserTokens_UserContacts_ContactId
FOREIGN KEY (ContactId) REFERENCES UserContacts(Id)
ON DELETE CASCADE;

-- Add index for verification token lookups
CREATE INDEX IX_UserTokens_ContactId_TokenType 
ON UserTokens(ContactId, TokenType)
WHERE ContactId IS NOT NULL;
```

### Configuration

Add to `appsettings.json`:

```json
{
  "DainnUser": {
    "RateLimiting": {
      "Rules": [
        {
          "Endpoint": "/api/profile/contacts/*/send-verification",
          "MaxRequests": 3,
          "WindowSeconds": 3600,
          "Mode": "PerUser"
        },
        {
          "Endpoint": "/api/profile/contacts/*/verify",
          "MaxRequests": 10,
          "WindowSeconds": 3600,
          "Mode": "PerUser"
        }
      ]
    }
  }
}
```

---

## Future Enhancements

1. **Bulk Operations:**
   - Import contacts from CSV
   - Export contacts

2. **Contact Groups:**
   - Organize contacts into groups (Family, Work, etc.)

3. **Contact History:**
   - Track changes to contacts over time

4. **Advanced Verification:**
   - Voice call verification for phone numbers
   - Link-based verification for email

5. **Contact Preferences:**
   - Preferred contact method
   - Do-not-contact flags

---

## References

- **PSA-77:** Address Management (reference implementation)
- **OWASP A07:** Authentication Failures (verification code security)
- **RFC 3966:** E.164 phone number format
- **RFC 5322:** Email address format
