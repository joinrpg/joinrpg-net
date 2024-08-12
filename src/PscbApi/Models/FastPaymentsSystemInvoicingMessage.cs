using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
///
/// </summary>
/// <remarks>
/// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-invojsing
/// </remarks>
public class FastPaymentsSystemInvoicingMessage : PaymentMessage
{
    [JsonProperty("marketPlace")]
    public string MerchantId { get; set; }

    [JsonProperty("expirationFromNow", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int? ExpirationMinutes { get; set; }

    [JsonProperty("expirationDateTime", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? ExpiredAt { get; set; }

    /// <inheritdoc cref="PaymentMessage.Data" />
    public new FastPaymentSystemInvoicingMessageData Data
    {
        get => (FastPaymentSystemInvoicingMessageData)base.Data;
        set => base.Data = value;
    }
}
