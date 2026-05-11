using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DainnUser.Core.Authorization;
using DainnUser.Core.Entities;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DainnUser.Infrastructure.Services;

/// <summary>
/// JWT token service implementation using HMAC-SHA256.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly DainnUserOptions _dainnUserOptions;
    private readonly SigningCredentials _signingCredentials;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="jwtOptions">JWT configuration options.</param>
    /// <param name="dainnUserOptions">General DainnUser options (used for access-token expiration).</param>
    public JwtTokenService(IOptions<JwtOptions> jwtOptions, DainnUserOptions dainnUserOptions)
    {
        _jwtOptions = jwtOptions.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
        _dainnUserOptions = dainnUserOptions ?? throw new ArgumentNullException(nameof(dainnUserOptions));

        if (string.IsNullOrWhiteSpace(_jwtOptions.Secret))
        {
            throw new InvalidOperationException(
                "JWT secret is not configured. Set 'DainnUser:Jwt:Secret' in your configuration.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(_jwtOptions.Secret);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT secret must be at least 32 bytes (256 bits) for HMAC-SHA256.");
        }

        _signingKey = new SymmetricSecurityKey(keyBytes)
        {
            KeyId = "DainnUserSigningKey"
        };
        _signingCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    /// <inheritdoc/>
    public AccessTokenResult GenerateAccessToken(User user, IEnumerable<string> roles, Guid sessionId)
    {
        return GenerateAccessToken(user, roles, Array.Empty<string>(), sessionId);
    }

    /// <inheritdoc/>
    public AccessTokenResult GenerateAccessToken(
        User user,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        Guid sessionId)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);
        ArgumentNullException.ThrowIfNull(permissions);

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_dainnUserOptions.JwtExpirationMinutes);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("sid", sessionId.ToString()),
            new Claim("email_verified", user.EmailVerified ? "true" : "false")
        };

        foreach (var role in roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
        }

        foreach (var permission in permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(DainnUserClaimTypes.Permission, permission.Trim().ToLowerInvariant()));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: _signingCredentials);

        var jwt = _tokenHandler.WriteToken(token);
        return new AccessTokenResult(jwt, expires);
    }

    /// <inheritdoc/>
    public string GenerateRefreshToken()
    {
        Span<byte> buffer = stackalloc byte[64];
        RandomNumberGenerator.Fill(buffer);
        return Base64UrlEncode(buffer);
    }

    /// <inheritdoc/>
    public string HashRefreshToken(string refreshToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var bytes = Encoding.UTF8.GetBytes(refreshToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <inheritdoc/>
    public ClaimsPrincipal? ValidateAccessToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = _jwtOptions.ValidateIssuer,
            ValidateAudience = _jwtOptions.ValidateAudience,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = _signingKey,
            ClockSkew = TimeSpan.FromSeconds(_jwtOptions.ClockSkewSeconds)
        };

        try
        {
            return _tokenHandler.ValidateToken(accessToken, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
