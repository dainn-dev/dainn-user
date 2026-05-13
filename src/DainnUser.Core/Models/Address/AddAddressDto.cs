namespace DainnUser.Core.Models.Address;

/// <summary>
/// Data transfer object for creating a new address.
/// </summary>
public class AddAddressDto
{
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
    /// Gets or sets whether to set this as the default address.
    /// </summary>
    public bool SetAsDefault { get; set; }
}
