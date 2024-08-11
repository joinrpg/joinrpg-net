using Newtonsoft.Json;

namespace PscbApi.Models;

public class RefundInfo : PaymentInfo
{
    /// <summary>
    /// Refund that was just created (as a result of refund operation)
    /// </summary>
    [JsonProperty("createdRefund")]
    public RefundData? CreatedRefund { get; set; }
}
