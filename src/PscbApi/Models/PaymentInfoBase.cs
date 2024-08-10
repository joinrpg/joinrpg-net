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
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Request (query) Id
    /// </summary>
    [JsonProperty("requestId")]
    public string? RequestId { get; set; }
}
