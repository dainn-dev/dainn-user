using System.Security.Cryptography;
using System.Text;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Contact;

namespace DainnUser.Application.Services;

/// <summary>
/// Service implementation for contact management.
/// </summary>
public class ContactService : IContactService
{
    private const int MaxVerificationRequestsPerHour = 3;
    private static readonly TimeSpan VerificationCodeLifetime = TimeSpan.FromMinutes(10);

    private readonly IUserRepository _userRepository;
    private readonly IContactRepository _contactRepository;
    private readonly IUserTokenRepository _tokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IContactVerificationSender> _verificationSenders;

    public ContactService(
        IUserRepository userRepository,
        IContactRepository contactRepository,
        IUserTokenRepository tokenRepository,
        IUnitOfWork unitOfWork,
        IEnumerable<IContactVerificationSender> verificationSenders)
    {
        _userRepository = userRepository;
        _contactRepository = contactRepository;
        _tokenRepository = tokenRepository;
        _unitOfWork = unitOfWork;
        _verificationSenders = verificationSenders;
    }

    public async Task<IReadOnlyList<ContactDto>> GetContactsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var contacts = await _contactRepository.GetByUserIdAsync(userId, cancellationToken);
        return contacts.Select(MapToDto).ToList();
    }

    public async Task<ContactDto> GetContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var contact = await GetContactForUserAsync(userId, contactId, cancellationToken);
        return MapToDto(contact);
    }

    public async Task<ContactDto> AddContactAsync(Guid userId, AddContactDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await EnsureUserExistsAsync(userId, cancellationToken);

        var now = DateTime.UtcNow;
        var contact = new UserContact
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContactType = dto.ContactType,
            ContactValue = dto.ContactValue,
            IsVerified = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (dto.SetAsPrimary)
        {
            await _contactRepository.ClearPrimaryForUserAndTypeAsync(userId, dto.ContactType, cancellationToken);
            contact.IsPrimary = true;
        }
        else
        {
            var hasContactsOfType = await _contactRepository.UserHasContactsOfTypeAsync(userId, dto.ContactType, cancellationToken);
            contact.IsPrimary = !hasContactsOfType;
        }

        await _contactRepository.AddAsync(contact, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(contact);
    }

    public async Task<ContactDto> UpdateContactAsync(Guid userId, Guid contactId, UpdateContactDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await EnsureUserExistsAsync(userId, cancellationToken);
        var contact = await GetContactForUserAsync(userId, contactId, cancellationToken);
        var originalType = contact.ContactType;

        if (dto.ContactType is not null)
        {
            contact.ContactType = dto.ContactType;
        }

        if (dto.ContactValue is not null && contact.ContactValue != dto.ContactValue)
        {
            contact.ContactValue = dto.ContactValue;
            contact.IsVerified = false;
        }

        if (!string.Equals(originalType, contact.ContactType, StringComparison.Ordinal) && contact.IsPrimary)
        {
            await _contactRepository.ClearPrimaryForUserAndTypeAsync(userId, contact.ContactType, cancellationToken);
        }

        contact.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(contact);
    }

    public async Task DeleteContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var contact = await GetContactForUserAsync(userId, contactId, cancellationToken);
        var wasPrimary = contact.IsPrimary;
        var contactType = contact.ContactType;

        if (wasPrimary)
        {
            var remaining = await _contactRepository.GetByUserIdAsync(userId, cancellationToken);
            var nextPrimary = remaining.FirstOrDefault(c => c.Id != contactId && c.ContactType == contactType);
            if (nextPrimary is not null)
            {
                nextPrimary.IsPrimary = true;
                nextPrimary.UpdatedAt = DateTime.UtcNow;
            }
        }

        _contactRepository.Remove(contact);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContactDto> SetPrimaryContactAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var contact = await GetContactForUserAsync(userId, contactId, cancellationToken);

        await _contactRepository.ClearPrimaryForUserAndTypeAsync(userId, contact.ContactType, cancellationToken);
        contact.IsPrimary = true;
        contact.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(contact);
    }

    public async Task SendVerificationCodeAsync(Guid userId, Guid contactId, CancellationToken cancellationToken = default)
    {
        await EnsureUserExistsAsync(userId, cancellationToken);
        var contact = await GetContactForUserAsync(userId, contactId, cancellationToken);
        var sender = _verificationSenders.FirstOrDefault(
            s => string.Equals(s.ContactType, contact.ContactType, StringComparison.OrdinalIgnoreCase));

        if (sender is null)
        {
            throw new NotSupportedException($"Verification is not supported for contact type '{contact.ContactType}'.");
        }

        var recentCount = await _tokenRepository.CountRecentContactVerificationTokensAsync(
            userId,
            contactId,
            DateTime.UtcNow.AddHours(-1),
            cancellationToken);

        if (recentCount >= MaxVerificationRequestsPerHour)
        {
            throw new TooManyVerificationAttemptsException();
        }

        var existingTokens = await _tokenRepository.GetActiveContactVerificationTokensAsync(userId, contactId, cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var token in existingTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = now;
        }

        var code = GenerateVerificationCode();
        await _tokenRepository.AddAsync(new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContactId = contactId,
            TokenType = TokenType.ContactVerification,
            TokenValue = HashCode(code),
            ExpiresAt = now.Add(VerificationCodeLifetime),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = now
        }, cancellationToken);

        await sender.SendVerificationCodeAsync(contact.ContactValue, code, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ContactDto> VerifyContactAsync(Guid userId, Guid contactId, string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        await EnsureUserExistsAsync(userId, cancellationToken);
        var contact = await GetContactForUserAsync(userId, contactId, cancellationToken);
        var codeHash = HashCode(code);
        var tokens = await _tokenRepository.GetActiveContactVerificationTokensAsync(userId, contactId, cancellationToken);
        var token = tokens.FirstOrDefault(t => t.TokenValue == codeHash);

        if (token is null)
        {
            throw new InvalidVerificationCodeException();
        }

        var now = DateTime.UtcNow;
        token.IsUsed = true;
        token.UsedAt = now;
        contact.IsVerified = true;
        contact.UpdatedAt = now;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapToDto(contact);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var exists = await _userRepository.AnyAsync(u => u.Id == userId, cancellationToken);
        if (!exists)
        {
            throw new UserNotFoundException(userId);
        }
    }

    private async Task<UserContact> GetContactForUserAsync(Guid userId, Guid contactId, CancellationToken cancellationToken)
    {
        var contact = await _contactRepository.GetByUserIdAndIdAsync(userId, contactId, cancellationToken);
        if (contact is null)
        {
            throw new ContactNotFoundException(contactId);
        }

        return contact;
    }

    private static ContactDto MapToDto(UserContact contact)
    {
        return new ContactDto
        {
            Id = contact.Id,
            ContactType = contact.ContactType,
            ContactValue = contact.ContactValue,
            IsVerified = contact.IsVerified,
            IsPrimary = contact.IsPrimary,
            CreatedAt = contact.CreatedAt,
            UpdatedAt = contact.UpdatedAt
        };
    }

    private static string GenerateVerificationCode()
    {
        return RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
    }

    private static string HashCode(string code)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    }
}
