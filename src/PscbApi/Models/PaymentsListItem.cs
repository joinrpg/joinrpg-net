using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// An element of the payments list returned by <c>merchantApi/getPayments</c>
/// </summary>
/// <remarks>
/// A derived type, and not extra properties on <see cref="PaymentData"/>: the base is what
/// <c>merchantApi/checkPayment</c> returns, and that response is parsed by the live payment status
/// path. A key carried by the list only must not be parsed there at all, because the same key is
/// shaped differently in the two responses — <c>lastError</c> is an object inside a list element and
/// an array in the <c>checkPayment</c> envelope — and a mismatch throws inside the payment update.
/// See ADR025.
/// </remarks>
public class PaymentsListItem : PaymentData
{
    /// <summary>
    /// Outcome of the last action with the payment, in the bank's own words — for a finished
    /// payment it reads "Платеж завершен" rather than describing an error. The bank omits the key
    /// for some of the rows
    /// </summary>
    [JsonProperty("lastError")]
    public BankResponseInfo? LastError { get; set; }
}
