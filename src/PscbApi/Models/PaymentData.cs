using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Payment data
/// </summary>
public class PaymentData
{
    /// <summary>
    /// Unique payment Id (in bank database)
    /// </summary>
    [JsonProperty("paymentId")]
    public string Id { get; set; }

    /// <summary>
    /// Order Id specified by store
    /// </summary>
    [JsonProperty("orderId")]
    public string OrderId { get; set; }

    /// <summary>
    /// Order Id as to be displayed to Payer
    /// </summary>
    [JsonProperty("showOrderId")]
    public string OrderIdDisplayValue { get; set; }

    /// <summary>
    /// Payment sum in RUR
    /// </summary>
    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Current payment status (at the moment of request)
    /// </summary>
    [JsonProperty("state")]
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Current payment sub status (at the moment of request)
    /// </summary>
    [JsonProperty("subState")]
    public PaymentSubStatus? SubStatus { get; set; }

    /// <summary>
    /// Date and time when payment enters to its current status
    /// </summary>
    [JsonProperty("stateDate")]
    public DateTimeOffset CurrentStatusDate { get; set; }

    /// <summary>
    /// Payment method. If not set, all payment methods will be used. User must to choose one
    /// at payment page
    /// </summary>
    [JsonProperty("paymentMethod")]
    public PscbPaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Identifier of a Payer issued by store
    /// </summary>
    [JsonProperty("account")]
    public string CustomerAccount { get; set; }

    /// <summary>
    /// Comment created by a Payer to hist payment
    /// </summary>
    [JsonProperty("comment")]
    public string CustomerComment { get; set; }

    /// <summary>
    /// Payer email
    /// </summary>
    [JsonProperty("email")]
    public string CustomerEmail { get; set; }

    /// <summary>
    /// Payer phone number in international format
    /// </summary>
    [JsonProperty("phone")]
    public string CustomerPhone { get; set; }

    /// <summary>
    /// Payment details
    /// </summary>
    [JsonProperty("details")]
    public string Details { get; set; }

    /// <summary>
    /// Token for subsequent payments
    /// </summary>
    [JsonProperty("recurrencyToken")]
    public string RecurrentPaymentToken { get; set; }
}
