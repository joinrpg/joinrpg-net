using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Recurrent payment information
/// </summary>
public class RecurrentPaymentInfo : PaymentInfo
{
    /// <summary>
    /// Recurrent payment error information
    /// </summary>
    [JsonProperty("paymentSystemError")]
    public RecurrentPaymentError? Error { get; set; }
}
