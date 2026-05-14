using DainnUser.OpenIddict.Configuration;
using DainnUser.OpenIddict.Data;
using DainnUser.OpenIddict.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace DainnUser.OpenIddict;

/// <summary>
/// Extension methods for adding the optional DainnUser OpenID Connect provider module.
/// </summary>
public static class DainnUserOpenIddictServiceExtensions
{
    /// <summary>
    /// Adds OpenIddict server support using a separate EF Core store context.
    /// </summary>
    public static IServiceCollection AddDainnUserOpenIddictProvider(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DainnUserOpenIddictOptions>? configureOptions = null)
    {
        var providerOptions = new DainnUserOpenIddictOptions();
        configuration.GetSection("DainnUser:OpenIddict").Bind(providerOptions);
        configureOptions?.Invoke(providerOptions);

        services.TryAddSingleton(providerOptions);
        services.TryAddSingleton<IOptions<DainnUserOpenIddictOptions>>(Options.Create(providerOptions));

        services.AddDainnUserOpenIddictDbContext(configuration);

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<DainnUserOpenIddictDbContext>();
            })
            .AddServer(options =>
            {
                if (providerOptions.Issuer is not null)
                {
                    options.SetIssuer(providerOptions.Issuer);
                }

                options
                    .SetAuthorizationEndpointUris(providerOptions.AuthorizationEndpointPath)
                    .SetTokenEndpointUris(providerOptions.TokenEndpointPath)
                    .SetUserInfoEndpointUris(providerOptions.UserInfoEndpointPath)
                    .SetEndSessionEndpointUris(providerOptions.LogoutEndpointPath);

                options
                    .AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .RequireProofKeyForCodeExchange();

                options.RegisterScopes(providerOptions.Scopes.ToArray());

                if (providerOptions.UseReferenceAccessTokens)
                {
                    options.UseReferenceAccessTokens();
                }
                else
                {
                    options.DisableAccessTokenEncryption();
                }

                if (providerOptions.UseDevelopmentCertificates)
                {
                    options
                        .AddDevelopmentEncryptionCertificate()
                        .AddDevelopmentSigningCertificate();
                }

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough();
            });

        services.AddHostedService<DainnUserOpenIddictClientSeeder>();

        return services;
    }

    private static IServiceCollection AddDainnUserOpenIddictDbContext(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var provider = configuration["DainnUser:OpenIddict:Database:Provider"]
            ?? configuration["DainnUser:Database:Provider"]
            ?? "SqlServer";
        var connectionString = configuration["DainnUser:OpenIddict:Database:ConnectionString"]
            ?? configuration["DainnUser:Database:ConnectionString"]
            ?? throw new InvalidOperationException("DainnUser OpenIddict database connection string is not configured.");

        services.AddDbContext<DainnUserOpenIddictDbContext>(options =>
        {
            switch (provider)
            {
                case "SqlServer":
                    options.UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(DainnUserOpenIddictDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    });
                    break;

                case "PostgreSQL":
                    options.UseNpgsql(connectionString, npgsqlOptions =>
                    {
                        npgsqlOptions.MigrationsAssembly(typeof(DainnUserOpenIddictDbContext).Assembly.FullName);
                        npgsqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    });
                    break;

                case "MySQL":
                    var serverVersion = ServerVersion.AutoDetect(connectionString);
                    options.UseMySql(connectionString, serverVersion, mySqlOptions =>
                    {
                        mySqlOptions.MigrationsAssembly(typeof(DainnUserOpenIddictDbContext).Assembly.FullName);
                        mySqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
                    });
                    break;

                case "SQLite":
                    options.UseSqlite(connectionString, sqliteOptions =>
                    {
                        sqliteOptions.MigrationsAssembly(typeof(DainnUserOpenIddictDbContext).Assembly.FullName);
                    });
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported DainnUser OpenIddict database provider: {provider}");
            }
        });

        return services;
    }
}
