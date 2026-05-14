namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Sends contact verification codes for a specific contact type.
/// </summary>
public interface IContactVerificationSender
{
    string ContactType { get; }
    Task SendVerificationCodeAsync(string contactValue, string code, CancellationToken cancellationToken = default);
}
