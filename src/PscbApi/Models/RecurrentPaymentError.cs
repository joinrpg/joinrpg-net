using JsonProperty = Newtonsoft.Json.JsonProperty;

namespace PscbApi.Models;

public class RecurrentPaymentError
{
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("subCode")]
    public string? SubCode { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }
}
