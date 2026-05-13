namespace DainnUser.Api.DTOs.Address;

/// <summary>
/// Request DTO for updating an existing address.
/// </summary>
public class UpdateAddressRequest
{
    /// <summary>
    /// Gets or sets the address type.
    /// </summary>
    public string? AddressType { get; set; }

    /// <summary>
    /// Gets or sets the first address line.
    /// </summary>
    public string? AddressLine1 { get; set; }

    /// <summary>
    /// Gets or sets the second address line.
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// Gets or sets the city.
    /// </summary>
    public string? City { get; set; }

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
    public string? Country { get; set; }
}
