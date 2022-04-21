// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models;

/// <summary>
/// Payment message
/// </summary>
/// <remarks>
/// See https://docs.pscb.ru/oos/api.html#api-magazina-sozdanie-platezha-parametry-zaprosa for details
/// </remarks>
public class PaymentMessage : IValidatableObject
{
    /// <summary>
    /// Minimal acceptable payment in RUR
    /// </summary>
    public const decimal MinPayment = 0.01M;

    /// <summary>
    /// Payment sum in RUR
    /// </summary>
    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Order Id specified by store
    /// </summary>
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Length must be from 4 to 20 characters")]
    [JsonProperty("orderId")]
    public string OrderId { get; set; }

    /// <summary>
    /// Order Id as to be displayed to Payer
    /// </summary>
    /// <remarks>
    /// If not set, value from <see cref="OrderId"/> used
    /// </remarks>
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Length must be from 4 to 20 characters")]
    [JsonProperty("showOrderId", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string OrderIdDisplayValue { get; set; }

    /// <summary>
    /// Payments details, will be shown to Payer during payment process
    /// </summary>
    [MaxLength(2048)]
    [JsonProperty("details", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string Details { get; set; }

    /// <summary>
    /// Payment method. If not set, all payment methods will be used. User must to choose one
    /// at payment page
    /// </summary>
    [JsonProperty("paymentMethod", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public PscbPaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Identifier of a Payer issued by store
    /// </summary>
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Length must be from 4 up to 20 characters")]
    [JsonProperty("customerAccount")]
    public string CustomerAccount { get; set; }

    /// <summary>
    /// Comment created by a Payer to hist payment
    /// </summary>
    [MaxLength(2048)]
    [JsonProperty("customerComment", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string CustomerComment { get; set; }

    /// <summary>
    /// Payer email
    /// </summary>
    [MaxLength(512)]
    [EmailAddress]
    [JsonProperty("customerEmail", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string CustomerEmail { get; set; }

    /// <summary>
    /// Payer phone number in international format
    /// </summary>
    [MaxLength(32)]
    [Phone]
    [JsonProperty("customerPhone", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string CustomerPhone { get; set; }

    /// <summary>
    /// Url to redirect after successful payment
    /// </summary>
    [MaxLength(1024)]
    [JsonProperty("successUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string SuccessUrl { get; set; }

    /// <summary>
    /// Url to redirect after failed payment
    /// </summary>
    [MaxLength(1024)]
    [JsonProperty("failUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string FailUrl { get; set; }

    /// <summary>
    /// Payment page language code according to ISO 639-1
    /// </summary>
    /// <remarks>
    /// Allowed values are: "en", "ru"
    /// </remarks>
    [MaxLength(2)]
    [JsonProperty("displayLanguage", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string UILanguage { get; set; }

    /// <summary>
    /// true if this payment has to be recurrent
    /// </summary>
    /// <remarks>
    /// See https://docs.pscb.ru/oos/advanced.html#dopolnitelnye-opcii-rekurrentnye-platezhi for details
    /// </remarks>
    [JsonProperty("recurrentable", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool? RecurrentPayment { get; set; }

    /// <summary>
    /// Additional data
    /// </summary>
    [Required]
    [JsonProperty("data")]
    public PaymentMessageData Data { get; set; }

    /// <summary>
    /// Random string
    /// </summary>
    public string nonce { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount < MinPayment)
        {
            yield return new ValidationResult($"{nameof(Amount)} could not be less than {MinPayment}", new[] { nameof(Amount) });
        }
    }
}
