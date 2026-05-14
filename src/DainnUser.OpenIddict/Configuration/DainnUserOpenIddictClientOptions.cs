namespace DainnUser.OpenIddict.Configuration;

/// <summary>
/// Describes an OIDC/OAuth client application managed by the DainnUser OpenIddict module.
/// </summary>
public class DainnUserOpenIddictClientOptions
{
    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret. Leave empty for public PKCE clients.
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this is a public client.
    /// </summary>
    public bool IsPublicClient { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether consent is required.
    /// </summary>
    public bool RequireConsent { get; set; } = true;

    /// <summary>
    /// Gets the allowed redirect URIs.
    /// </summary>
    public IList<Uri> RedirectUris { get; } = new List<Uri>();

    /// <summary>
    /// Gets the allowed post-logout redirect URIs.
    /// </summary>
    public IList<Uri> PostLogoutRedirectUris { get; } = new List<Uri>();

    /// <summary>
    /// Gets the permissions/scopes granted to the client.
    /// </summary>
    public ISet<string> Scopes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "openid",
        "profile",
        "email"
    };
}
