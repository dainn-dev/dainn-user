namespace DainnUser.Core.Enums;

/// <summary>
/// Represents external login providers for OAuth authentication.
/// </summary>
public enum LoginProvider
{
    /// <summary>
    /// Local authentication using email/username and password.
    /// </summary>
    Local = 0,

    /// <summary>
    /// Google OAuth provider.
    /// </summary>
    Google = 1,

    /// <summary>
    /// Facebook OAuth provider.
    /// </summary>
    Facebook = 2,

    /// <summary>
    /// GitHub OAuth provider.
    /// </summary>
    GitHub = 3,

    /// <summary>
    /// Microsoft OAuth provider.
    /// </summary>
    Microsoft = 4
}
