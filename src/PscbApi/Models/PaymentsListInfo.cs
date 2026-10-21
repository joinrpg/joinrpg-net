using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Response of the payments list request
/// </summary>
/// <remarks>
/// See https://docs.pscb.ru/oos/api.html#api-dopolnitelnyh-vozmozhnostej-zapros-spiska-platezhej
/// </remarks>
public class PaymentsListInfo : PaymentInfoBase
{
    /// <summary>
    /// Payments matching the query. Absent when the request failed
    /// </summary>
    [JsonProperty("payments")]
    public ICollection<PaymentData>? Payments { get; set; }
}
