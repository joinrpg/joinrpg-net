using Newtonsoft.Json;

namespace PscbApi.Models;

public class SetupFastPaymentsSystemRecurrentPaymentQueryParams
{
    [JsonProperty("orderId")]
    public required string OrderId { get; set; }

    [JsonProperty("marketPlace")]
    public required string MarketplaceId { get; set; }

    [JsonProperty("token")]
    public required string RecurrencyToken { get; set; }

    [JsonProperty("amount")]
    public required int Amount { get; set; }

    [JsonProperty("purpose")]
    public required string PaymentPurpose { get; set; }
}
