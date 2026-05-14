using DainnUser.Core.Configuration;

namespace DainnUser.Infrastructure.Configuration;

/// <summary>
/// Configuration options for request rate limiting.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether rate limiting is enabled.
    /// Combined with <see cref="DainnUserOptions.EnableRateLimiting"/> — both must be true for the middleware to apply limits.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the per-endpoint rate limit rules.
    /// </summary>
    public List<RateLimitRule> Rules { get; set; } = new();

    /// <summary>
    /// Gets or sets the IP addresses exempt from rate limiting (loopback, internal services, etc.).
    /// </summary>
    public List<string> WhitelistIps { get; set; } = new();
}

/// <summary>
/// A single rate limit rule applied to a path prefix or exact path.
/// </summary>
public class RateLimitRule
{
    /// <summary>
    /// Gets or sets the endpoint path. Exact match by default; ending the value with <c>*</c>
    /// performs a case-insensitive prefix match (e.g. <c>/api/auth/*</c>).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of permitted requests within <see cref="WindowSeconds"/>.
    /// </summary>
    public int MaxRequests { get; set; } = 60;

    /// <summary>
    /// Gets or sets the rolling window duration in seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Gets or sets the partitioning mode for this rule.
    /// </summary>
    public RateLimitMode Mode { get; set; } = RateLimitMode.PerIp;

    /// <summary>
    /// Gets or sets the number of segments in the sliding window. Higher values give smoother
    /// behavior but cost more memory. Defaults to 6.
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 6;
}

/// <summary>
/// Partitioning strategy for a rate limit rule.
/// </summary>
public enum RateLimitMode
{
    /// <summary>
    /// Limit per client IP address (anonymous).
    /// </summary>
    PerIp = 0,

    /// <summary>
    /// Limit per authenticated user (falls back to IP when user is not authenticated).
    /// </summary>
    PerUser = 1,

    /// <summary>
    /// Limit per (IP + user) pair — strictest combination.
    /// </summary>
    PerIpAndUser = 2
}
