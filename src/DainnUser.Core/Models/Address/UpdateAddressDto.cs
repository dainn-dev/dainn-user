namespace DainnUser.Core.Models.Address;

/// <summary>
/// Data transfer object for updating an existing address.
/// </summary>
public class UpdateAddressDto
{
    /// <summary>
    /// Gets or sets the type of address (e.g., Home, Work, Billing, Shipping).
    /// </summary>
    public string? AddressType { get; set; }

    /// <summary>
    /// Gets or sets the first line of the address.
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Gets or sets the second line of the address (optional).
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string? City { get; set; }

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
    public string? Country { get; set; }
}
