using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Bank card data
/// </summary>
public class BankCardData
{
    /// <summary>
    /// Bank that emits card
    /// </summary>
    [JsonProperty("card_bank")]
    public string Bank { get; set; }

    /// <summary>
    /// Payment status in processing
    /// </summary>
    [JsonProperty("state_desc")]
    public string ProcessingStatus { get; set; }

    /// <summary>
    /// Country where card was emitted
    /// </summary>
    [JsonProperty("card_country_name")]
    public string Country { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-3 country code
    /// </summary>
    [JsonProperty("card_country_isoa3")]
    public string CountryCode { get; set; }

    /// <summary>
    /// Card expiration date
    /// </summary>
    [JsonProperty("card_exp")]
    public string ExpirationDate { get; set; }

    /// <summary>
    /// Payment system and card type
    /// </summary>
    [JsonProperty("card_type")]
    public string Type { get; set; }

    /// <summary>
    /// Masked card number (PAN)
    /// </summary>
    [JsonProperty("card")]
    public string MaskedName { get; set; }

    /// <summary>
    /// Reference Retrieval Number -- unique number of transaction
    /// </summary>
    [JsonProperty("rrn")]
    public string ReferenceRetrievalNumber { get; set; }
}
