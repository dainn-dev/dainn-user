namespace DainnUser.Core.Entities;

/// <summary>
/// Represents a physical address associated with a user.
/// </summary>
public class UserAddress
{
    /// <summary>
    /// Gets or sets the unique identifier for the address.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the user identifier.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the address type (e.g., "Home", "Work", "Billing", "Shipping").
    /// </summary>
    public string AddressType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the street address line 1.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the street address line 2 (optional).
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province.
    /// </summary>
    public string? StateProvince { get; set; }

    /// <summary>
    /// Gets or sets the postal or ZIP code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether this is the default address.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the address was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date and time when the address was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties

    /// <summary>
    /// Gets or sets the user associated with this address.
    /// </summary>
    public User User { get; set; } = null!;
}
