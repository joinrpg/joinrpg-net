using Newtonsoft.Json;

namespace PscbApi.Models;

public class CancelFastPaymentsSystemRecurrentPaymentQueryParams
{
    [JsonProperty("orderId")]
    public required string OrderId { get; set; }

    [JsonProperty("marketPlace")]
    public required string MarketplaceId { get; set; }

    [JsonProperty("token")]
    public required string RecurrencyToken { get; set; }
}
