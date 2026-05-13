namespace DainnUser.Core.Exceptions;

/// <summary>
/// Thrown when an address cannot be found by the provided identifier.
/// </summary>
public class AddressNotFoundException : Exception
{
    /// <summary>
    /// Gets the address identifier that could not be found.
    /// </summary>
    public Guid AddressId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AddressNotFoundException"/> class.
    /// </summary>
    /// <param name="addressId">The address identifier that could not be found.</param>
    public AddressNotFoundException(Guid addressId)
        : base($"Address with id '{addressId}' was not found.")
    {
        AddressId = addressId;
    }
}
