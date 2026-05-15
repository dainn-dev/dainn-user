namespace DainnUser.Core.Configuration;

/// <summary>
/// Configuration for a generic OpenID Connect (OIDC) provider.
/// </summary>
public class OidcProviderConfig
{
    /// <summary>
    /// Gets or sets the unique provider identifier (e.g., "auth0", "okta", "keycloak").
    /// Used in URLs and database records.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for UI rendering (e.g., "Auth0", "Okta").
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OIDC authority URL (e.g., https://your-tenant.auth0.com).
    /// The middleware will append /.well-known/openid-configuration for discovery.
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth 2.0 client identifier.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the OAuth 2.0 client secret.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the callback/redirect URI path (default: "/signin-oidc").
    /// Must match the redirect URI configured in the OIDC provider.
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Gets or sets the space-separated OIDC scopes to request (default: "openid profile email").
    /// </summary>
    public string Scope { get; set; } = "openid profile email";

    /// <summary>
    /// Gets or sets the claim type to use for user email (default: "email").
    /// </summary>
    public string EmailClaimType { get; set; } = "email";

    /// <summary>
    /// Gets or sets the claim type to use for user name (default: "name").
    /// </summary>
    public string NameClaimType { get; set; } = "name";

    /// <summary>
    /// Gets or sets the claim type to use for unique user identifier (default: "sub").
    /// </summary>
    public string SubjectClaimType { get; set; } = "sub";

    /// <summary>
    /// Gets or sets the claim type used by the provider to indicate email verification (default: "email_verified").
    /// </summary>
    public string EmailVerifiedClaimType { get; set; } = "email_verified";

    /// <summary>
    /// Gets or sets a value indicating whether the email must be verified by the OIDC provider
    /// before an existing local account can be automatically linked to it (default: true).
    /// Disable only for providers that guarantee verified emails without the claim.
    /// </summary>
    public bool RequireEmailVerifiedForAutoLink { get; set; } = true;
}
