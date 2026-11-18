namespace JoinRpg.XGameApi.Contract.Pscb;

/// <summary>
/// A single payment as the PSCB bank knows it
/// </summary>
/// <remarks>
/// <para>
/// Field names are ours. Values of the coded fields — <see cref="Status"/>, <see cref="SubStatus"/>,
/// <see cref="Kind"/> and <see cref="PaymentMethod"/> — are the bank's own identifiers, so they can
/// be compared with the bank's documentation and with the merchant's cabinet directly.
/// See https://docs.pscb.ru/oos/api.html
/// </para>
/// <para>
/// Every field is optional: the bank returns a different subset of them depending on the payment
/// state and on the flags of the request.
/// </para>
/// </remarks>
public class PscbPaymentInfo
{
    /// <summary>
    /// Payment identifier in the bank's database
    /// </summary>
    public string? PaymentId { get; set; }

    /// <summary>
    /// Payment identifier in the payment system. Present for finished and refunded payments
    /// </summary>
    public string? TransactionId { get; set; }

    /// <summary>
    /// Payment identifier on the merchant side. For JoinRpg this is the finance operation id
    /// padded with zeroes to 10 digits
    /// </summary>
    public string? OrderId { get; set; }

    /// <summary>
    /// Payment identifier as it is shown to the payer
    /// </summary>
    public string? DisplayOrderId { get; set; }

    /// <summary>
    /// Marketplace the payment belongs to
    /// </summary>
    public string? MarketPlace { get; set; }

    /// <summary>
    /// Payment sum, RUR
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Commission paid by the payer, RUR
    /// </summary>
    public decimal? Fee { get; set; }

    /// <summary>
    /// Payment status: new, sent, end, ref, exp, hold, canceled, err, rej, undef, done
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Payment sub-status, e.g. "hold_confirmed"
    /// </summary>
    public string? SubStatus { get; set; }

    /// <summary>
    /// Kind of the record: "payment" or "invoice"
    /// </summary>
    public string? Kind { get; set; }

    /// <summary>
    /// Payment method identifier, e.g. "ac" for a bank card or "sbp" for the Fast Payments System
    /// </summary>
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Moment when the payment entered its current status
    /// </summary>
    public DateTimeOffset StatusDate { get; set; }

    /// <summary>
    /// Payer identifier issued by the merchant
    /// </summary>
    public string? CustomerAccount { get; set; }

    /// <summary>
    /// Comment the payer has left
    /// </summary>
    public string? CustomerComment { get; set; }

    /// <summary>
    /// Payer's email
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Payer's phone number in international format
    /// </summary>
    public string? CustomerPhone { get; set; }

    /// <summary>
    /// Payment details (purpose of payment)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Arbitrary data the merchant has attached to the payment, as compact JSON.
    /// Their structure is not defined by the bank
    /// </summary>
    public string? MerchantData { get; set; }

    /// <summary>
    /// Token for recurrent payments. Present only for successful payments created as recurrentable
    /// </summary>
    public string? RecurrentPaymentToken { get; set; }

    /// <summary>
    /// Number of the payments registry the money was settled by. Present only when clearing data
    /// was requested and the payment is finished
    /// </summary>
    public string? RegistryId { get; set; }

    /// <summary>
    /// Date of the payments registry the money was settled by
    /// </summary>
    public DateTimeOffset? RegistryDate { get; set; }

    /// <summary>
    /// Extended bank card data
    /// </summary>
    public PscbBankCardInfo? Card { get; set; }

    /// <summary>
    /// Refunds of this payment
    /// </summary>
    public IReadOnlyCollection<PscbRefundInfo> Refunds { get; set; } = [];

    /// <summary>
    /// Fiscal receipts of this payment. Present only when fiscal data was requested
    /// </summary>
    public IReadOnlyCollection<PscbReceiptInfo> Receipts { get; set; } = [];
}
