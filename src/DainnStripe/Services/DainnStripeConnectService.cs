using DainnStripe.Data;
using DainnStripe.Entities;
using DainnStripe.Enums;
using DainnStripe.Interfaces;
using DainnStripe.Models;
using Microsoft.EntityFrameworkCore;

namespace DainnStripe.Services;

/// <summary>
/// Default Connect tenant and connected account service.
/// </summary>
public class DainnStripeConnectService : IDainnStripeConnectService
{
    private readonly IDainnStripeConnectAccountClient _connectAccountClient;
    private readonly DainnStripeDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="DainnStripeConnectService"/> class.
    /// </summary>
    public DainnStripeConnectService(
        IDainnStripeConnectAccountClient connectAccountClient,
        DainnStripeDbContext dbContext)
    {
        _connectAccountClient = connectAccountClient;
        _dbContext = dbContext;
    }

    /// <inheritdoc />
    public async Task<DainnStripeTenant> UpsertTenantAsync(
        UpsertTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.TenantId, nameof(request.TenantId));
        EnsureRequired(request.DisplayName, nameof(request.DisplayName));
        EnsureRequired(request.DefaultCurrency, nameof(request.DefaultCurrency));

        var tenant = await _dbContext.DainnStripeTenants
            .SingleOrDefaultAsync(item => item.TenantId == request.TenantId, cancellationToken);

        var now = DateTime.UtcNow;
        if (tenant is null)
        {
            tenant = new DainnStripeTenant
            {
                Id = Guid.NewGuid(),
                TenantId = request.TenantId,
                CreatedAt = now
            };
            _dbContext.DainnStripeTenants.Add(tenant);
        }

        tenant.DisplayName = request.DisplayName;
        tenant.DefaultCurrency = request.DefaultCurrency.ToLowerInvariant();
        tenant.Active = request.Active;
        tenant.MetadataJson = request.MetadataJson;
        tenant.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    /// <inheritdoc />
    public async Task<DainnStripeConnectedAccount> CreateConnectedAccountAsync(
        CreateConnectedAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.TenantId, nameof(request.TenantId));
        EnsureRequired(request.OwnerId, nameof(request.OwnerId));
        EnsureRequired(request.Country, nameof(request.Country));
        EnsureRequired(request.DefaultCurrency, nameof(request.DefaultCurrency));

        var tenant = await _dbContext.DainnStripeTenants
            .SingleOrDefaultAsync(item => item.TenantId == request.TenantId, cancellationToken)
            ?? throw new InvalidOperationException($"DainnStripe tenant '{request.TenantId}' does not exist.");

        var existing = await _dbContext.DainnStripeConnectedAccounts
            .SingleOrDefaultAsync(
                item => item.TenantId == tenant.Id && item.OwnerId == request.OwnerId,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var result = await _connectAccountClient.CreateAccountAsync(request, cancellationToken);
        var now = DateTime.UtcNow;
        var account = new DainnStripeConnectedAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            OwnerId = request.OwnerId,
            StripeAccountId = result.StripeAccountId,
            Email = result.Email ?? request.Email,
            Country = result.Country ?? request.Country,
            DefaultCurrency = (result.DefaultCurrency ?? request.DefaultCurrency).ToLowerInvariant(),
            ChargesEnabled = result.ChargesEnabled,
            PayoutsEnabled = result.PayoutsEnabled,
            DetailsSubmitted = result.DetailsSubmitted,
            Status = ResolveStatus(result.ChargesEnabled, result.PayoutsEnabled, result.DetailsSubmitted),
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.DainnStripeConnectedAccounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    /// <inheritdoc />
    public async Task<ConnectedAccountLinkResult> CreateOnboardingLinkAsync(
        CreateConnectedAccountLinkRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.StripeAccountId, nameof(request.StripeAccountId));
        EnsureRequired(request.ReturnUrl, nameof(request.ReturnUrl));
        EnsureRequired(request.RefreshUrl, nameof(request.RefreshUrl));

        var account = await _dbContext.DainnStripeConnectedAccounts
            .SingleOrDefaultAsync(item => item.StripeAccountId == request.StripeAccountId, cancellationToken)
            ?? throw new InvalidOperationException(
                $"DainnStripe connected account '{request.StripeAccountId}' does not exist.");

        var result = await _connectAccountClient.CreateAccountLinkAsync(request, cancellationToken);
        account.OnboardingUrl = result.Url;
        account.OnboardingUrlExpiresAt = result.ExpiresAt;
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<DainnStripeConnectedAccount?> SyncConnectedAccountAsync(
        SyncConnectedAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        EnsureRequired(request.StripeAccountId, nameof(request.StripeAccountId));

        var account = await _dbContext.DainnStripeConnectedAccounts
            .SingleOrDefaultAsync(item => item.StripeAccountId == request.StripeAccountId, cancellationToken);

        if (account is null)
        {
            return null;
        }

        account.ChargesEnabled = request.ChargesEnabled;
        account.PayoutsEnabled = request.PayoutsEnabled;
        account.DetailsSubmitted = request.DetailsSubmitted;
        account.Status = request.Status
            ?? ResolveStatus(request.ChargesEnabled, request.PayoutsEnabled, request.DetailsSubmitted);
        account.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return account;
    }

    private static DainnStripeConnectedAccountStatus ResolveStatus(
        bool chargesEnabled,
        bool payoutsEnabled,
        bool detailsSubmitted)
    {
        if (chargesEnabled && payoutsEnabled)
        {
            return DainnStripeConnectedAccountStatus.Active;
        }

        return detailsSubmitted
            ? DainnStripeConnectedAccountStatus.Restricted
            : DainnStripeConnectedAccountStatus.OnboardingRequired;
    }

    private static void EnsureRequired(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }
    }
}
