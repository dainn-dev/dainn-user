namespace DainnUser.Api.DTOs.Address;

/// <summary>
/// Request DTO for adding a new address.
/// </summary>
public class AddAddressRequest
{
    /// <summary>
    /// Gets or sets the address type.
    /// </summary>
    public string AddressType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first address line.
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the second address line.
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
    /// Gets or sets the postal code.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Gets or sets the country.
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to set this as the default address.
    /// </summary>
    public bool SetAsDefault { get; set; }
}
