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
    public PaymentData? Payment { get; set; }

    /// <summary>
    /// List of errors (if any)
    /// </summary>
    [JsonProperty("lastError")]
    public ICollection<BankResponseInfo>? Errors { get; set; }
}
