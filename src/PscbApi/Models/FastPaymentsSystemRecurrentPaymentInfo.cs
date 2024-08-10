using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Information about configured recurrent payment
/// </summary>
public class FastPaymentsSystemRecurrentPaymentInfo : PaymentInfoBase
{
    [JsonProperty("qrcId")]
    public string? FastPaymentSystemRecurrencyId { get; set; }

    [JsonProperty("paymentPurpose")]
    public string? PaymentPurpose { get; set; }

    [JsonProperty("amount")]
    public decimal? Amount { get; set; }
}
