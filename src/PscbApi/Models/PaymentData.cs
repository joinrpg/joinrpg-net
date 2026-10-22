using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PscbApi.Models;

/// <summary>
/// Payment data
/// </summary>
/// <remarks>
/// This is the shape <c>merchantApi/checkPayment</c> returns inside <c>payment</c>, and it is parsed
/// by the live payment status path. Keys that only the payments list carries are declared in
/// <see cref="PaymentsListItem"/> instead, so that they are never parsed there. See ADR025.
/// </remarks>
public class PaymentData
{
    /// <summary>
    /// Unique payment Id (in bank database)
    /// </summary>
    [JsonProperty("paymentId")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Payment Id in the payment system. Returned by the bank for payments in the
    /// <see cref="PaymentStatus.Paid"/> or <see cref="PaymentStatus.Refunded"/> status
    /// </summary>
    [JsonProperty("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>
    /// Order Id specified by store
    /// </summary>
    [JsonProperty("orderId")]
    public string OrderId { get; set; } = null!;

    /// <summary>
    /// Order Id as to be displayed to Payer
    /// </summary>
    [JsonProperty("showOrderId")]
    public string? OrderIdDisplayValue { get; set; }

    /// <summary>
    /// Payment sum in RUR
    /// </summary>
    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Commission paid by the Payer, in RUR
    /// </summary>
    [JsonProperty("fee")]
    public decimal? Fee { get; set; }

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
    /// Kind of the record: a payment or an invoice
    /// </summary>
    [JsonProperty("type")]
    public PscbPaymentType? Type { get; set; }

    /// <summary>
    /// Marketplace the payment belongs to
    /// </summary>
    [JsonProperty("marketPlace")]
    public string? MarketPlace { get; set; }

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
    public string? CustomerAccount { get; set; }

    /// <summary>
    /// Comment created by a Payer to hist payment
    /// </summary>
    [JsonProperty("comment")]
    public string? CustomerComment { get; set; }

    /// <summary>
    /// Payer email
    /// </summary>
    [JsonProperty("email")]
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Payer phone number in international format
    /// </summary>
    [JsonProperty("phone")]
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Payment details
    /// </summary>
    [JsonProperty("details")]
    public string? Details { get; set; }

    /// <summary>
    /// Arbitrary parameters attached to the payment by the store. Their structure is not defined by the bank
    /// </summary>
    /// <remarks>
    /// Typed as <see cref="JToken"/>, not <see cref="JObject"/>: the shape belongs to whoever
    /// created the payment, so a string or an array in it must not break parsing of the response.
    /// </remarks>
    [JsonProperty("merchantData")]
    public JToken? MerchantData { get; set; }

    /// <summary>
    /// Token for subsequent payments
    /// </summary>
    [JsonProperty("recurrencyToken")]
    public string? RecurrentPaymentToken { get; set; }

    /// <summary>
    /// List of refunds (if any)
    /// </summary>
    [JsonProperty("refunds")]
    public ICollection<RefundData>? Refunds { get; set; }

    /// <summary>
    /// List of receipts (if any)
    /// </summary>
    [JsonProperty("receipts")]
    public ICollection<ReceiptData>? Receipts { get; set; }

    /// <summary>
    /// Extended bank card information
    /// </summary>
    [JsonProperty("paymentParams")]
    public BankCardData? Card { get; set; } // TODO: Implement object structure

    /// <summary>
    /// Number of the payments registry the money was settled by.
    /// Returned only when clearing data was requested and the payment is finished
    /// </summary>
    [JsonProperty("registryId")]
    public string? RegistryId { get; set; }

    /// <summary>
    /// Date of the payments registry the money was settled by.
    /// Returned only when clearing data was requested and the payment is finished
    /// </summary>
    [JsonProperty("registryDate")]
    public DateTimeOffset? RegistryDate { get; set; }
}
