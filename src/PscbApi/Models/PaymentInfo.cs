using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Base class for payment information
/// </summary>
public class PaymentInfoBase
{
    /// <summary>
    /// Query status
    /// </summary>
    [JsonProperty("status")]
    public PaymentInfoQueryStatus Status { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    [JsonProperty("errorCode")]
    public ApiErrorCode? ErrorCode { get; set; }

    /// <summary>
    /// Error description
    /// </summary>
    [JsonProperty("errorDescription")]
    public string ErrorDescription { get; set; }

    /// <summary>
    /// Request (query) Id
    /// </summary>
    [JsonProperty("requestId")]
    public string RequestId { get; set; }
}

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
    public ICollection<RefundData> Refunds { get; set; }

    /// <summary>
    /// List of receipts (if any)
    /// </summary>
    [JsonProperty("receipts")]
    public ICollection<ReceiptData> Receipts { get; set; }

    /// <summary>
    /// Extended bank card information
    /// </summary>
    [JsonProperty("paymentParams")]
    public BankCardData Card { get; set; } // TODO: Implement object structure

    /// <summary>
    /// List of errors (if any)
    /// </summary>
    [JsonProperty("lastError")]
    public ICollection<BankResponseInfo> Errors { get; set; }
}
