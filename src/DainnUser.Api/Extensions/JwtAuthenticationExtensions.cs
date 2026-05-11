using System.IdentityModel.Tokens.Jwt;
using System.Text;
using DainnUser.Core.Authorization;
using DainnUser.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DainnUser.Api.Extensions;

/// <summary>
/// Extension methods for configuring JWT bearer authentication.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>
    /// Registers JWT bearer authentication using DainnUser's JWT options.
    /// Reads <c>DainnUser:Jwt</c> from configuration and binds <see cref="JwtOptions"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddDainnUserJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection("DainnUser:Jwt").Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Secret))
        {
            throw new InvalidOperationException(
                "DainnUser:Jwt:Secret is not configured. Cannot wire JWT bearer authentication.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(jwt.Secret);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "DainnUser:Jwt:Secret must be at least 32 bytes (256 bits) for HMAC-SHA256.");
        }

        var signingKey = new SymmetricSecurityKey(keyBytes)
        {
            KeyId = "DainnUserSigningKey"
        };

        // Clear the default inbound claim type map to prevent JWT claims from being transformed
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = jwt.ValidateIssuer,
                    ValidateAudience = jwt.ValidateAudience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkewSeconds)
                };

                // Set MapInboundClaims = false to use JwtSecurityTokenHandler (compatible
                // with JwtTokenService) instead of the default JsonWebTokenHandler.
                options.MapInboundClaims = false;
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(DainnUserPolicies.CanReadUsers, policy =>
                policy.RequireClaim(DainnUserClaimTypes.Permission, DainnUserPermissions.UsersRead));
            options.AddPolicy(DainnUserPolicies.CanWriteUsers, policy =>
                policy.RequireClaim(DainnUserClaimTypes.Permission, DainnUserPermissions.UsersWrite));
            options.AddPolicy(DainnUserPolicies.CanDeleteUser, policy =>
                policy.RequireClaim(DainnUserClaimTypes.Permission, DainnUserPermissions.UsersDelete));
            options.AddPolicy(DainnUserPolicies.CanManageRoles, policy =>
                policy.RequireClaim(DainnUserClaimTypes.Permission, DainnUserPermissions.RolesWrite));
            options.AddPolicy(DainnUserPolicies.CanDeleteRole, policy =>
                policy.RequireClaim(DainnUserClaimTypes.Permission, DainnUserPermissions.RolesDelete));
            options.AddPolicy(DainnUserPolicies.CanManageSettings, policy =>
                policy.RequireClaim(DainnUserClaimTypes.Permission, DainnUserPermissions.SettingsWrite));
        });

        return services;
    }
}
