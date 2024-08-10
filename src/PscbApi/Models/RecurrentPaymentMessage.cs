using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace PscbApi.Models;

public class RecurrentPaymentMessage : IValidatableObject
{
    /// <summary>
    /// Minimal acceptable payment in RUR
    /// </summary>
    public const decimal MinPayment = 0.01M;

    /// <summary>
    /// Payment sum in RUR
    /// </summary>
    [JsonProperty("amount", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public decimal? Amount { get; set; }

    /// <summary>
    /// Parent order Id (from the initial payment)
    /// </summary>
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Length must be from 4 to 20 characters")]
    [JsonProperty("orderId")]
    public required string ParentOrderId { get; set; }

    /// <summary>
    /// New order Id
    /// </summary>
    [StringLength(20, MinimumLength = 4, ErrorMessage = "Length must be from 4 to 20 characters")]
    [JsonProperty("newOrderId")]
    public required string OrderId { get; set; }

    [Required]
    [JsonProperty("token")]
    public required string RecurrencyToken { get; set; }

    [Required]
    [JsonProperty("marketPlace")]
    public required string MerchantId { get; set; }

    /// <summary>
    /// Payments details. If not set, a value from the parent payment will be used
    /// </summary>
    [MaxLength(2048)]
    [JsonProperty("details", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? Details { get; set; }

    /// <summary>
    /// Additional data
    /// </summary>
    [Required]
    [JsonProperty("data")]
    public required PaymentMessageData Data { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount < MinPayment)
        {
            yield return new ValidationResult($"{nameof(Amount)} could not be less than {MinPayment}", new[] { nameof(Amount) });
        }
    }
}
