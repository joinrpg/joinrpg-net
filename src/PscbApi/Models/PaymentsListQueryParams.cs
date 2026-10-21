using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Query for the list of payments of a marketplace
/// </summary>
/// <remarks>
/// The bank accepts at most one month per request. Splitting a longer period is the caller's job,
/// see <see cref="PaymentsPeriodSplitter"/>.
/// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-zapros-spiska-platezhej
/// </remarks>
public class PaymentsListQueryParams
{
    /// <summary>
    /// Merchant Id (issued by the bank)
    /// </summary>
    [Required]
    [JsonProperty("marketPlace")]
    public required string MerchantId { get; set; }

    /// <summary>
    /// Lower bound of the requested period, inclusive
    /// </summary>
    [Required]
    [JsonProperty("dateFrom")]
    [JsonConverter(typeof(PscbDateTimeOffsetConverter))]
    public DateTimeOffset DateFrom { get; set; }

    /// <summary>
    /// Upper bound of the requested period, exclusive
    /// </summary>
    [JsonProperty("dateTo", NullValueHandling = NullValueHandling.Ignore)]
    [JsonConverter(typeof(PscbDateTimeOffsetConverter))]
    public DateTimeOffset? DateTo { get; set; }

    /// <summary>
    /// Selection by payment status. When not set, the bank uses <see cref="PaymentsSelectMode.Paid"/>
    /// </summary>
    [JsonProperty("selectMode", NullValueHandling = NullValueHandling.Ignore)]
    public PaymentsSelectMode? SelectMode { get; set; }

    /// <summary>
    /// Selection by payment kind. When not set, the bank returns payments and invoices together
    /// </summary>
    [JsonProperty("paymentType", NullValueHandling = NullValueHandling.Ignore)]
    public PscbPaymentType? PaymentType { get; set; }

    /// <summary>
    /// If true, fiscal documents associated with the payments will be returned
    /// </summary>
    [JsonProperty("requestFiscalData")]
    public bool RequestFiscalData { get; set; }

    /// <summary>
    /// If true, clearing data (registry number and registry date) will be returned
    /// </summary>
    [JsonProperty("requestClearingData")]
    public bool RequestClearingData { get; set; }
}
