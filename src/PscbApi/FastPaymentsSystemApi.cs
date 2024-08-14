using Newtonsoft.Json;

namespace PscbApi;

public class FastPaymentsSystemApi
{
    private readonly IHttpClientFactory clientFactory;

    /// <summary>
    /// true if API is configured to use debug endpoint
    /// </summary>
    public bool Debug => true;

    public FastPaymentsSystemApi(IHttpClientFactory clientFactory)
    {
        this.clientFactory = clientFactory;
    }

    /// <summary>
    /// Retrieves a list of banks that are connected with the fast payments system.
    /// </summary>
    /// <param name="platform">Platform</param>
    /// <param name="client">Client identification (phone number)</param>
    /// <param name="paymentUrl">QR code url</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task<ICollection<FpsBank>> GetFastPaymentsSystemBanks(FpsPlatform platform, string client, string paymentUrl)
    {
        var httpClient = clientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "https://widget.cbrpay.ru/v1/members");
        request.Headers.Add("X-CLIENT", client);
        request.Headers.Add("X-PLATFORM", platform.GetIdentifier());
        request.Headers.Add("X-PAYLOAD", paymentUrl);

        HttpResponseMessage httpResponse = await httpClient.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync();
        System.Diagnostics.Debug.WriteLineIf(Debug, responseJson);

        return JsonConvert.DeserializeObject<FpsBanks>(responseJson)?.Banks ?? throw new InvalidOperationException("Error parsing FPS result");
    }

}

public enum FpsPlatform
{
    [Identifier("desktop")]
    Desktop,

    [Identifier("android")]
    Android,

    [Identifier("ios")]
    Ios,
}

public class FpsBank
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("logo")]
    public string LogoUrl { get; set; }

    [JsonProperty("url")]
    public string PaymentUrl { get; set; }
}

public class FpsBanks
{
    [JsonProperty("version")]
    public string Version { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("platform")]
    public FpsPlatform Platform { get; set; }

    [JsonProperty("members")]
    public ICollection<FpsBank> Banks { get; set; }
}
