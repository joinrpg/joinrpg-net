using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Payment information
/// </summary>
public class PaymentInfo : PaymentInfoBase
{
    /// <summary>
    /// Payment data
    /// </summary>
    [JsonProperty("payment")]
    public PaymentData Payment { get; set; }

    /// <summary>
    /// List of refunds (if any)
    /// </summary>
    [JsonProperty("refunds")]
    public ICollection<RefundData>? Refunds { get; set; }

    /// <summary>
    /// List of receipts (if any)
    /// </summary>
    [JsonProperty("receipts")]
    public ICollection<ReceiptData>? Receipts { get; set; }

    /// <summary>
    /// Extended bank card information
    /// </summary>
    [JsonProperty("paymentParams")]
    public BankCardData? Card { get; set; } // TODO: Implement object structure

    /// <summary>
    /// List of errors (if any)
    /// </summary>
    [JsonProperty("lastError")]
    public ICollection<BankResponseInfo>? Errors { get; set; }
}
