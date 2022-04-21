// ReSharper disable IdentifierTypo
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Error information
/// </summary>
public class BankResponseInfo
{

#pragma warning disable 1591
    internal const string PaymentsSystemCodeJsonName = "code";
    internal const string ProcessingCenterCodeJsonName = "subCode";
#pragma warning restore 1591

    /// <summary>
    /// Error description
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    /// Error code from the payments system
    /// </summary>
    [JsonProperty(PaymentsSystemCodeJsonName)]
    public PaymentsSystemResponseCode? PaymentCode { get; set; }

    /// <summary>
    /// Error code from the processing center
    /// </summary>
    [JsonProperty(ProcessingCenterCodeJsonName)]
    public ProcessingCenterResponseCode? ProcessingCode { get; set; }
}
