using Newtonsoft.Json;

namespace PscbApi.Models;

public class FastPaymentsSystemInvoicingInfo : PaymentInfoBase
{
    /// <summary>
    /// Payment data
    /// </summary>
    [JsonProperty("payment")]
    public FastPaymentsSystemInvoicingData Payment { get; set; }
}
