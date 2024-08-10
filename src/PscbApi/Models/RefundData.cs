using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Refund data, associated with payment
/// </summary>
public class RefundData
{
    /// <summary>
    /// Refund operation Id
    /// </summary>
    [JsonProperty("refundId")]
    public string Id { get; set; }

    /// <summary>
    /// Refund status
    /// </summary>
    [JsonProperty("state")]
    public RefundStatus Status { get; set; }
}

public class RefundInfo : PaymentInfoBase
{
    [JsonProperty("payment")]
    public RefundPaymentInfo Payment { get; set; }
}

public class RefundPaymentInfo
{
    /// <summary>
    /// The refund that was just created
    /// </summary>
    [JsonProperty("createdRefund")]
    public RefundData CreatedRefund { get; set; }
}
