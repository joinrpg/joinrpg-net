using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Kind of a payment record: a payment itself or an invoice
/// </summary>
/// <remarks>
/// Used both as the <c>type</c> key of a payment object and as the <c>paymentType</c> filter
/// of the payments list request. Prefixed with <c>Pscb</c> the same way as
/// <see cref="PscbPaymentMethod"/>: an unprefixed <c>PaymentType</c> collides with
/// <c>JoinRpg.DataModel.PaymentType</c> wherever both namespaces are in scope.
/// </remarks>
[JsonConverter(typeof(IdentifiableEnumConverter))]
public enum PscbPaymentType
{
    /// <summary>
    /// Regular payment
    /// </summary>
    [Identifier("payment")]
    Payment,

    /// <summary>
    /// Invoice
    /// </summary>
    [Identifier("invoice")]
    Invoice,
}
