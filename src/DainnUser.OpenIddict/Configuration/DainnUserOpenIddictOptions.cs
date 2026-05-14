namespace DainnUser.OpenIddict.Configuration;

/// <summary>
/// Configuration options for the optional DainnUser OpenID Connect provider module.
/// </summary>
public class DainnUserOpenIddictOptions
{
    /// <summary>
    /// Gets or sets the issuer URI exposed in OIDC discovery metadata.
    /// </summary>
    public Uri? Issuer { get; set; }

    /// <summary>
    /// Gets or sets the authorization endpoint path.
    /// </summary>
    public string AuthorizationEndpointPath { get; set; } = "/connect/authorize";

    /// <summary>
    /// Gets or sets the token endpoint path.
    /// </summary>
    public string TokenEndpointPath { get; set; } = "/connect/token";

    /// <summary>
    /// Gets or sets the userinfo endpoint path.
    /// </summary>
    public string UserInfoEndpointPath { get; set; } = "/connect/userinfo";

    /// <summary>
    /// Gets or sets the logout endpoint path.
    /// </summary>
    public string LogoutEndpointPath { get; set; } = "/connect/logout";

    /// <summary>
    /// Gets or sets a value indicating whether development certificates should be registered.
    /// Production hosts should disable this and register persisted signing/encryption certificates.
    /// </summary>
    public bool UseDevelopmentCertificates { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether reference access tokens should be used.
    /// </summary>
    public bool UseReferenceAccessTokens { get; set; } = false;

    /// <summary>
    /// Gets or sets scopes exposed by the provider.
    /// </summary>
    public ISet<string> Scopes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "openid",
        "profile",
        "email",
        "roles",
        "offline_access"
    };

    /// <summary>
    /// Gets the clients to seed into the OpenIddict application store.
    /// </summary>
    public IList<DainnUserOpenIddictClientOptions> Clients { get; } = new List<DainnUserOpenIddictClientOptions>();
}
