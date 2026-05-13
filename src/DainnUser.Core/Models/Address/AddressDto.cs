namespace DainnUser.Core.Models.Address;

/// <summary>
/// Data transfer object representing an address.
/// </summary>
public class AddressDto
{
    /// <summary>
    /// Gets or sets the unique identifier for the address.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the type of address (e.g., Home, Work, Billing, Shipping).
    /// </summary>
    public string AddressType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first line of the address.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the second line of the address (optional).
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state or province (optional).
    /// </summary>
    public string? StateProvince { get; set; }

    /// <summary>
    /// Gets or sets the postal code (optional).
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this is the default address.
    /// </summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the address was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the address was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
