using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Query for payment status
/// </summary>
public class PaymentInfoQueryParams
{
    /// <summary>
    /// Order Id
    /// </summary>
    [Required]
    [JsonProperty("orderId")]
    public required string OrderId { get; set; }

    /// <summary>
    /// Merchant Id (issued by the bank)
    /// </summary>
    [Required]
    [JsonProperty("marketPlace")]
    public required string MerchantId { get; set; }

    /// <summary>
    /// If true, additional card data will be returned
    /// </summary>
    [JsonProperty("requestCardData")]
    public bool GetCardData { get; set; }

    /// <summary>
    /// If true, fiscal documents associated with payment will be returned
    /// </summary>
    [JsonProperty("requestFiscalData")]
    public bool GetFiscalData { get; set; }
}
