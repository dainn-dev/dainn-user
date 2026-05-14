using DainnUser.OpenIddict.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace DainnUser.OpenIddict.Services;

/// <summary>
/// Seeds configured OpenIddict client applications.
/// </summary>
public class DainnUserOpenIddictClientSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DainnUserOpenIddictOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnUserOpenIddictClientSeeder"/> class.
    /// </summary>
    public DainnUserOpenIddictClientSeeder(
        IServiceProvider serviceProvider,
        DainnUserOpenIddictOptions options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_options.Clients.Count == 0)
        {
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in _options.Clients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId))
            {
                continue;
            }

            var existing = await manager.FindByClientIdAsync(client.ClientId, cancellationToken);
            if (existing is not null)
            {
                continue;
            }

            var descriptor = CreateDescriptor(client);
            await manager.CreateAsync(descriptor, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static OpenIddictApplicationDescriptor CreateDescriptor(DainnUserOpenIddictClientOptions client)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = client.ClientId,
            ClientSecret = client.IsPublicClient ? null : client.ClientSecret,
            ClientType = client.IsPublicClient ? ClientTypes.Public : ClientTypes.Confidential,
            ConsentType = client.RequireConsent ? ConsentTypes.Explicit : ConsentTypes.Implicit,
            DisplayName = client.DisplayName
        };

        descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(Permissions.Endpoints.Token);
        descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
        descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

        foreach (var scope in client.Scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)))
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }

        foreach (var uri in client.RedirectUris)
        {
            descriptor.RedirectUris.Add(uri);
        }

        foreach (var uri in client.PostLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(uri);
        }

        return descriptor;
    }
}
