using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using DainnUser.Application.Services;
using DainnUser.Core.Entities;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Contact;
using FluentAssertions;
using Moq;

namespace DainnUser.UnitTests.Services;

public class ContactServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IContactRepository> _contactRepository = new();
    private readonly Mock<IUserTokenRepository> _tokenRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IContactVerificationSender> _sender = new();
    private readonly ContactService _service;

    public ContactServiceTests()
    {
        _unitOfWork.Setup(x => x.SaveChangesAsync(default)).ReturnsAsync(1);
        _sender.SetupGet(x => x.ContactType).Returns("Email");
        _service = new ContactService(
            _userRepository.Object,
            _contactRepository.Object,
            _tokenRepository.Object,
            _unitOfWork.Object,
            [_sender.Object]);
    }

    [Fact]
    public async Task GetContactsAsync_WhenUserNotFound_ThrowsUserNotFoundException()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), default)).ReturnsAsync(false);

        var act = () => _service.GetContactsAsync(userId);

        await act.Should().ThrowAsync<UserNotFoundException>();
    }

    [Fact]
    public async Task AddContactAsync_FirstContactOfType_SetsPrimary()
    {
        var userId = Guid.NewGuid();
        UserExists();
        _contactRepository.Setup(x => x.UserHasContactsOfTypeAsync(userId, "Email", default)).ReturnsAsync(false);

        var result = await _service.AddContactAsync(userId, new AddContactDto { ContactType = "Email", ContactValue = "user@example.com" });

        result.IsPrimary.Should().BeTrue();
        _contactRepository.Verify(x => x.AddAsync(It.Is<UserContact>(c => c.IsPrimary), default), Times.Once);
    }

    [Fact]
    public async Task UpdateContactAsync_WhenValueChanges_MarksUnverified()
    {
        var userId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var contact = new UserContact
        {
            Id = contactId,
            UserId = userId,
            ContactType = "Email",
            ContactValue = "old@example.com",
            IsVerified = true
        };
        UserExists();
        _contactRepository.Setup(x => x.GetByUserIdAndIdAsync(userId, contactId, default)).ReturnsAsync(contact);

        var result = await _service.UpdateContactAsync(userId, contactId, new UpdateContactDto { ContactValue = "new@example.com" });

        result.ContactValue.Should().Be("new@example.com");
        result.IsVerified.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteContactAsync_PrimaryContact_PromotesRemainingContactOfSameType()
    {
        var userId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var deleted = new UserContact { Id = contactId, UserId = userId, ContactType = "Email", ContactValue = "old@example.com", IsPrimary = true };
        var remaining = new UserContact { Id = Guid.NewGuid(), UserId = userId, ContactType = "Email", ContactValue = "next@example.com" };
        UserExists();
        _contactRepository.Setup(x => x.GetByUserIdAndIdAsync(userId, contactId, default)).ReturnsAsync(deleted);
        _contactRepository.Setup(x => x.GetByUserIdAsync(userId, default)).ReturnsAsync([remaining]);

        await _service.DeleteContactAsync(userId, contactId);

        remaining.IsPrimary.Should().BeTrue();
        _contactRepository.Verify(x => x.Remove(deleted), Times.Once);
    }

    [Fact]
    public async Task SetPrimaryContactAsync_ClearsOtherPrimaryAndSetsRequestedContact()
    {
        var userId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var contact = new UserContact { Id = contactId, UserId = userId, ContactType = "Email", ContactValue = "user@example.com" };
        UserExists();
        _contactRepository.Setup(x => x.GetByUserIdAndIdAsync(userId, contactId, default)).ReturnsAsync(contact);

        var result = await _service.SetPrimaryContactAsync(userId, contactId);

        result.IsPrimary.Should().BeTrue();
        _contactRepository.Verify(x => x.ClearPrimaryForUserAndTypeAsync(userId, "Email", default), Times.Once);
    }

    [Fact]
    public async Task SendVerificationCodeAsync_WhenRecentTokensReachedLimit_ThrowsTooManyVerificationAttemptsException()
    {
        var userId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        UserExists();
        _contactRepository.Setup(x => x.GetByUserIdAndIdAsync(userId, contactId, default)).ReturnsAsync(
            new UserContact { Id = contactId, UserId = userId, ContactType = "Email", ContactValue = "user@example.com" });
        _tokenRepository.Setup(x => x.CountRecentContactVerificationTokensAsync(userId, contactId, It.IsAny<DateTime>(), default)).ReturnsAsync(3);

        var act = () => _service.SendVerificationCodeAsync(userId, contactId);

        await act.Should().ThrowAsync<TooManyVerificationAttemptsException>();
        _sender.Verify(x => x.SendVerificationCodeAsync(It.IsAny<string>(), It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task SendVerificationCodeAsync_SendsSixDigitCodeAndStoresHashOnly()
    {
        var userId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        UserToken? savedToken = null;
        string? sentCode = null;
        UserExists();
        _contactRepository.Setup(x => x.GetByUserIdAndIdAsync(userId, contactId, default)).ReturnsAsync(
            new UserContact { Id = contactId, UserId = userId, ContactType = "Email", ContactValue = "user@example.com" });
        _tokenRepository.Setup(x => x.CountRecentContactVerificationTokensAsync(userId, contactId, It.IsAny<DateTime>(), default)).ReturnsAsync(0);
        _tokenRepository.Setup(x => x.GetActiveContactVerificationTokensAsync(userId, contactId, default)).ReturnsAsync([]);
        _tokenRepository.Setup(x => x.AddAsync(It.IsAny<UserToken>(), default)).Callback<UserToken, CancellationToken>((t, _) => savedToken = t);
        _sender.Setup(x => x.SendVerificationCodeAsync("user@example.com", It.IsRegex("^[0-9]{6}$"), default))
            .Callback<string, string, CancellationToken>((_, code, _) => sentCode = code)
            .Returns(Task.CompletedTask);

        await _service.SendVerificationCodeAsync(userId, contactId);

        sentCode.Should().NotBeNull();
        savedToken.Should().NotBeNull();
        savedToken!.TokenType.Should().Be(TokenType.ContactVerification);
        savedToken.ContactId.Should().Be(contactId);
        savedToken.TokenValue.Should().Be(Hash(sentCode!));
        savedToken.TokenValue.Should().NotBe(sentCode);
    }

    [Fact]
    public async Task VerifyContactAsync_WhenCodeMatchesActiveHash_MarksContactVerifiedAndTokenUsed()
    {
        var userId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        var contact = new UserContact { Id = contactId, UserId = userId, ContactType = "Email", ContactValue = "user@example.com" };
        var token = new UserToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ContactId = contactId,
            TokenType = TokenType.ContactVerification,
            TokenValue = Hash("123456"),
            ExpiresAt = DateTime.UtcNow.AddMinutes(10)
        };
        UserExists();
        _contactRepository.Setup(x => x.GetByUserIdAndIdAsync(userId, contactId, default)).ReturnsAsync(contact);
        _tokenRepository.Setup(x => x.GetActiveContactVerificationTokensAsync(userId, contactId, default)).ReturnsAsync([token]);

        var result = await _service.VerifyContactAsync(userId, contactId, "123456");

        result.IsVerified.Should().BeTrue();
        token.IsUsed.Should().BeTrue();
        token.UsedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task VerifyContactAsync_WhenCodeDoesNotMatch_ThrowsInvalidVerificationCodeException()
    {
        var userId = Guid.NewGuid();
        var contactId = Guid.NewGuid();
        UserExists();
        _contactRepository.Setup(x => x.GetByUserIdAndIdAsync(userId, contactId, default)).ReturnsAsync(
            new UserContact { Id = contactId, UserId = userId, ContactType = "Email", ContactValue = "user@example.com" });
        _tokenRepository.Setup(x => x.GetActiveContactVerificationTokensAsync(userId, contactId, default)).ReturnsAsync([]);

        var act = () => _service.VerifyContactAsync(userId, contactId, "123456");

        await act.Should().ThrowAsync<InvalidVerificationCodeException>();
    }

    private void UserExists()
    {
        _userRepository.Setup(x => x.AnyAsync(It.IsAny<Expression<Func<User, bool>>>(), default)).ReturnsAsync(true);
    }

    private static string Hash(string code)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
    }
}
