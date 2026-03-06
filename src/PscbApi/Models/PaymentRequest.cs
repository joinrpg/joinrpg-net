// ReSharper disable InconsistentNaming
namespace PscbApi.Models;

/// <summary>
/// Payment request: https://docs.pscb.ru/oos/api.html#api-magazina-sozdanie-platezha
/// </summary>
/// <remarks>
/// Do not rename properties to pascal-case
/// </remarks>
public class PaymentRequest
{
    /// <summary>
    /// Marketplace identifier
    /// </summary>
    public string marketPlace { get; set; } = null!;

    /// <summary>
    /// Encoded payment message
    /// </summary>
    public string message { get; set; } = null!;

    /// <summary>
    /// Payment message signature
    /// </summary>
    public string signature { get; set; } = null!;
}
