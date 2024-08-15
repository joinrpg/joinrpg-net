using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models;

public class RefundMessage
{
    [Required]
    [JsonProperty("marketPlace")]
    public required string MerchantId { get; set; }

    /// <summary>
    /// New order Id
    /// </summary>
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Length must be from 4 to 20 characters")]
    [JsonProperty("orderId")]
    public required string OrderId { get; set; }

    [JsonProperty("partialRefund")]
    public bool PartialRefund { get; set; }

    /// <summary>
    /// Payment sum in RUR. Required when <see cref="PartialRefund"/> is true
    /// </summary>
    [JsonProperty("refundSum", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public decimal? Amount { get; set; }

    [JsonProperty("data")]
    public required RefundMessageData Data { get; set; }
}

public class RefundMessageData
{
    [JsonProperty("fdReceipt")]
    public Receipt? Receipt { get; set; }
}
