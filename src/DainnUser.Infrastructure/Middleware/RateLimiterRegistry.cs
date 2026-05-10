using System.Threading.RateLimiting;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DainnUser.Infrastructure.Middleware;

/// <summary>
/// Holds one <see cref="PartitionedRateLimiter{TResource}"/> per configured rule and resolves
/// which rule (if any) applies to an incoming request path. Registered as a singleton so
/// limiter state survives across requests.
/// </summary>
public class RateLimiterRegistry : IDisposable
{
    private readonly List<RuleEntry> _entries;
    private readonly HashSet<string> _whitelistIps;
    private readonly RateLimitingOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes the registry from <see cref="RateLimitingOptions"/>.
    /// </summary>
    /// <param name="options">Rate-limiting configuration.</param>
    public RateLimiterRegistry(IOptions<RateLimitingOptions> options)
    {
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));

        _entries = new List<RuleEntry>(_options.Rules.Count);
        foreach (var rule in _options.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Endpoint))
            {
                throw new InvalidOperationException(
                    "Rate limit rule has empty Endpoint. Configure 'DainnUser:RateLimiting:Rules[*].Endpoint'.");
            }
            if (rule.MaxRequests <= 0)
            {
                throw new InvalidOperationException(
                    $"Rate limit rule for '{rule.Endpoint}' has non-positive MaxRequests ({rule.MaxRequests}).");
            }
            if (rule.WindowSeconds <= 0)
            {
                throw new InvalidOperationException(
                    $"Rate limit rule for '{rule.Endpoint}' has non-positive WindowSeconds ({rule.WindowSeconds}).");
            }

            _entries.Add(BuildEntry(rule));
        }

        _whitelistIps = new HashSet<string>(_options.WhitelistIps, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a value indicating whether rate limiting is configured and active.
    /// </summary>
    public bool IsEnabled => _options.Enabled && _entries.Count > 0;

    /// <summary>
    /// Returns true if the given IP is exempt from rate limiting.
    /// </summary>
    /// <param name="ipAddress">The client IP address (may be null).</param>
    public bool IsWhitelisted(string? ipAddress)
        => !string.IsNullOrWhiteSpace(ipAddress) && _whitelistIps.Contains(ipAddress);

    /// <summary>
    /// Resolves the first matching rule for the given request path, or null if no rule matches.
    /// </summary>
    /// <param name="requestPath">The request path (case-insensitive).</param>
    public RuleEntry? Resolve(string? requestPath)
    {
        if (string.IsNullOrEmpty(requestPath))
        {
            return null;
        }

        foreach (var entry in _entries)
        {
            if (entry.Matches(requestPath))
            {
                return entry;
            }
        }

        return null;
    }

    private static RuleEntry BuildEntry(RateLimitRule rule)
    {
        var endpoint = rule.Endpoint.Trim();
        var isPrefix = endpoint.EndsWith('*');
        var matchValue = isPrefix ? endpoint[..^1] : endpoint;

        var segmentsPerWindow = Math.Max(1, rule.SegmentsPerWindow);

        var limiter = PartitionedRateLimiter.Create<string, string>(key =>
            RateLimitPartition.GetSlidingWindowLimiter(key, _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = rule.MaxRequests,
                Window = TimeSpan.FromSeconds(rule.WindowSeconds),
                SegmentsPerWindow = segmentsPerWindow,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            }));

        return new RuleEntry(rule, matchValue, isPrefix, limiter);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        foreach (var entry in _entries)
        {
            entry.Limiter.Dispose();
        }
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// A compiled rule with its matcher and limiter.
    /// </summary>
    public sealed class RuleEntry
    {
        internal RuleEntry(RateLimitRule rule, string matchValue, bool isPrefix, PartitionedRateLimiter<string> limiter)
        {
            Rule = rule;
            MatchValue = matchValue;
            IsPrefix = isPrefix;
            Limiter = limiter;
        }

        /// <summary>The original rule configuration.</summary>
        public RateLimitRule Rule { get; }

        /// <summary>The path or path-prefix this rule matches.</summary>
        public string MatchValue { get; }

        /// <summary>True if MatchValue is a prefix; false for exact match.</summary>
        public bool IsPrefix { get; }

        /// <summary>The underlying rate limiter for this rule.</summary>
        public PartitionedRateLimiter<string> Limiter { get; }

        /// <summary>Returns true if this rule applies to the given request path.</summary>
        public bool Matches(string requestPath)
        {
            return IsPrefix
                ? requestPath.StartsWith(MatchValue, StringComparison.OrdinalIgnoreCase)
                : string.Equals(requestPath, MatchValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}
